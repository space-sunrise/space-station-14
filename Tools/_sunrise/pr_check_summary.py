#!/usr/bin/env python3

import argparse
import html
import io
import json
import os
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
import zipfile
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any


GITHUB_API_URL = os.environ.get("GITHUB_API_URL", "https://api.github.com")
COMMENT_MARKER = "<!-- sunrise-pr-check-summary -->"
COMMENT_SOFT_LIMIT = 62000
MAX_ERROR_CHARS = 9000
MAX_LOG_CHARS = 12000

SUCCESS_CONCLUSIONS = {"success", "skipped", "neutral"}
FAILURE_CONCLUSIONS = {"failure", "cancelled", "timed_out", "action_required", "startup_failure"}
PAGINATED_RESPONSE_KEYS = ("workflow_runs", "jobs", "items", "check_runs", "artifacts")


@dataclass
class FailureDetails:
    error_text: str
    test_names: list[str]
    log_text: str


@dataclass
class CheckRow:
    name: str
    status: str
    url: str = ""
    failure: FailureDetails | None = None


@dataclass(frozen=True)
class JobConfig:
    job_name: str
    display_name: str


@dataclass(frozen=True)
class WorkflowConfig:
    workflow_name: str
    jobs: tuple[JobConfig, ...]
    show_when_missing: bool = False


TRACKED_WORKFLOWS: tuple[WorkflowConfig, ...] = (
    WorkflowConfig(
        "YAML Linter",
        (JobConfig("YAML Linter", "YAML Linter"),),
        show_when_missing=True,
    ),
    WorkflowConfig(
        "Build & Test Debug",
        (
            JobConfig("build", "Build & Test Debug / Build"),
            JobConfig("Content Tests", "Content Tests"),
            *(JobConfig(f"Integration Tests (shard {i})", f"Integration Tests (shard {i})") for i in range(8)),
        ),
        show_when_missing=True,
    ),
    WorkflowConfig(
        "Test Packaging",
        (JobConfig("Test Packaging", "Test Packaging"),),
    ),
    WorkflowConfig(
        "CRLF Check",
        (JobConfig("CRLF Check", "CRLF Check"),),
    ),
    WorkflowConfig(
        "Map file schema validator",
        (JobConfig("YAML map schema validator", "YAML map schema validator"),),
    ),
    WorkflowConfig(
        "Locale Validator",
        (JobConfig("Validate Locales", "Validate Locales"),),
    ),
    WorkflowConfig(
        "RGA schema validator",
        (JobConfig("YAML RGA schema validator", "YAML RGA schema validator"),),
    ),
    WorkflowConfig(
        "RSI Validator",
        (JobConfig("Validate RSIs", "Validate RSIs"),),
    ),
    WorkflowConfig(
        "No submodule update checker",
        (JobConfig("Submodule update in pr found", "No submodule update checker"),),
    ),
)

TRACKED_WORKFLOW_NAMES = {workflow.workflow_name for workflow in TRACKED_WORKFLOWS}


def main() -> int:
    args = parse_args()
    token = os.environ.get("GITHUB_TOKEN")
    repository = os.environ.get("GITHUB_REPOSITORY")
    event_name = os.environ.get("GITHUB_EVENT_NAME", "")
    event_path = os.environ.get("GITHUB_EVENT_PATH")

    if not token or not repository or not event_path:
        print("GITHUB_TOKEN, GITHUB_REPOSITORY and GITHUB_EVENT_PATH are required.", file=sys.stderr)
        return 1

    with open(event_path, encoding="utf-8") as f:
        payload = json.load(f)

    client = GitHubClient(token, repository)
    try:
        pull_request = resolve_pull_request(client, payload, event_name, args.pr_number)

        if pull_request is None:
            print("No matching open pull request found, skipping.")
            return 0

        pr_number = pull_request["number"]
        head_sha = pull_request["head"]["sha"]
        rows = collect_check_rows(client, head_sha)
        body = build_comment(pr_number, head_sha, rows)
        upsert_comment(client, pr_number, body)
    except RuntimeError as error:
        print(error, file=sys.stderr)
        return 1

    print(f"Updated PR check summary comment for #{pr_number} at {head_sha}.")
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Update the Sunrise PR check summary comment.")
    parser.add_argument("--pr-number", type=int, default=None)
    return parser.parse_args()


class GitHubClient:
    def __init__(self, token: str, repository: str):
        self.token = token
        self.repository = repository

    def request(
        self,
        method: str,
        path: str,
        *,
        params: dict[str, Any] | None = None,
        body: dict[str, Any] | None = None,
        accept: str = "application/vnd.github+json",
        return_headers: bool = False,
    ) -> Any:
        url = path if urllib.parse.urlparse(path).scheme else f"{GITHUB_API_URL}{path}"
        if params:
            query = urllib.parse.urlencode({k: v for k, v in params.items() if v is not None})
            if query:
                url = f"{url}?{query}"

        data = None
        headers = self.headers(accept)
        if body is not None:
            data = json.dumps(body).encode("utf-8")
            headers["Content-Type"] = "application/json"

        request = urllib.request.Request(url, data=data, method=method, headers=headers)
        try:
            with urllib.request.urlopen(request) as response:
                raw = response.read()
                response_headers = response.headers
        except urllib.error.HTTPError as error:
            details = error.read().decode("utf-8", "replace")
            raise RuntimeError(f"GitHub API {method} {path} failed: {error.code} {details}") from error

        result = None
        if not raw:
            return (result, response_headers) if return_headers else result

        result = json.loads(raw.decode("utf-8"))
        return (result, response_headers) if return_headers else result

    def paginate(self, path: str, *, params: dict[str, Any] | None = None) -> list[Any]:
        request_params = dict(params or {})
        request_params.setdefault("per_page", 100)
        request_path: str | None = path
        results: list[Any] = []

        while request_path:
            data, headers = self.request("GET", request_path, params=request_params, return_headers=True)
            page_items = paginated_items(data)

            results.extend(page_items)
            request_path = next_link_from_headers(headers)
            request_params = None

        return results

    def download_job_log(self, job_id: int) -> str:
        url = f"{GITHUB_API_URL}/repos/{self.repository}/actions/jobs/{job_id}/logs"
        request = urllib.request.Request(url, method="GET", headers=self.headers("application/vnd.github+json"))
        with urllib.request.urlopen(request) as response:
            data = response.read()

        if data.startswith(b"PK"):
            with zipfile.ZipFile(io.BytesIO(data)) as archive:
                chunks = []
                for name in archive.namelist():
                    with archive.open(name) as f:
                        chunks.append(f.read().decode("utf-8", "replace"))
                return "\n".join(chunks)

        return data.decode("utf-8", "replace")

    def headers(self, accept: str) -> dict[str, str]:
        return {
            "Authorization": f"Bearer {self.token}",
            "Accept": accept,
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "sunrise-pr-check-summary",
        }


def paginated_items(data: Any) -> list[Any]:
    if isinstance(data, dict):
        for key in PAGINATED_RESPONSE_KEYS:
            if key in data:
                return data[key] or []
        keys = ", ".join(PAGINATED_RESPONSE_KEYS)
        raise RuntimeError(f"GitHub paginated response did not contain any known collection key: {keys}.")

    return data or []


def next_link_from_headers(headers: Any) -> str | None:
    link_header = headers.get("Link") or headers.get("link") or ""
    for part in link_header.split(","):
        segments = [segment.strip() for segment in part.split(";")]
        if len(segments) < 2:
            continue

        if 'rel="next"' not in segments[1:]:
            continue

        url = segments[0]
        if url.startswith("<") and url.endswith(">"):
            return url[1:-1]

    return None


def resolve_pull_request(
    client: GitHubClient,
    payload: dict[str, Any],
    event_name: str,
    explicit_pr_number: int | None,
) -> dict[str, Any] | None:
    pr_number = explicit_pr_number
    workflow_run_head_sha = None

    if event_name in {"pull_request", "pull_request_target"}:
        pr_number = payload.get("pull_request", {}).get("number")
    elif event_name == "workflow_run":
        workflow_run = payload.get("workflow_run", {})
        if workflow_run.get("event") not in {"pull_request", "pull_request_target"}:
            return None
        workflow_run_head_sha = workflow_run.get("head_sha")
        pull_requests = workflow_run.get("pull_requests") or []
        if pull_requests:
            pr_number = pull_requests[0].get("number")
        elif workflow_run_head_sha:
            pr_number = find_pull_request_by_head_sha(client, workflow_run_head_sha)

    if not pr_number:
        return None

    pull_request = client.request("GET", f"/repos/{client.repository}/pulls/{pr_number}")
    if pull_request.get("state") != "open":
        return None

    if workflow_run_head_sha and pull_request["head"]["sha"] != workflow_run_head_sha:
        print(
            f"Workflow run belongs to stale SHA {workflow_run_head_sha}; "
            f"current PR head is {pull_request['head']['sha']}. Skipping."
        )
        return None

    return pull_request


def find_pull_request_by_head_sha(client: GitHubClient, head_sha: str) -> int | None:
    pulls = client.request(
        "GET",
        f"/repos/{client.repository}/commits/{head_sha}/pulls",
        accept="application/vnd.github+json",
    )
    for pull_request in pulls or []:
        if pull_request.get("state") == "open":
            return pull_request.get("number")
    return None


def collect_check_rows(client: GitHubClient, head_sha: str) -> list[CheckRow]:
    runs = client.paginate(
        f"/repos/{client.repository}/actions/runs",
        params={"head_sha": head_sha, "exclude_pull_requests": "false"},
    )
    latest_runs = latest_tracked_runs(runs)
    rows: list[CheckRow] = []

    for workflow in TRACKED_WORKFLOWS:
        run = latest_runs.get(workflow.workflow_name)
        if run is None:
            if workflow.show_when_missing:
                rows.extend(CheckRow(job.display_name, "in_progress") for job in workflow.jobs)
            continue

        jobs = get_jobs_for_run(client, run)
        rows.extend(rows_for_workflow(client, workflow, run, jobs))

    return rows


def latest_tracked_runs(runs: list[dict[str, Any]]) -> dict[str, dict[str, Any]]:
    latest: dict[str, dict[str, Any]] = {}

    for run in runs:
        name = run.get("name")
        if name not in TRACKED_WORKFLOW_NAMES:
            continue
        if run.get("event") not in {"pull_request", "pull_request_target"}:
            continue

        current = latest.get(name)
        if current is None or run_sort_key(run) > run_sort_key(current):
            latest[name] = run

    return latest


def run_sort_key(run: dict[str, Any]) -> tuple[str, int, int]:
    return (
        run.get("created_at") or "",
        int(run.get("run_attempt") or 0),
        int(run.get("id") or 0),
    )


def get_jobs_for_run(client: GitHubClient, run: dict[str, Any]) -> list[dict[str, Any]]:
    run_id = run["id"]
    return client.paginate(
        f"/repos/{client.repository}/actions/runs/{run_id}/jobs",
        params={"filter": "latest"},
    )


def rows_for_workflow(
    client: GitHubClient,
    workflow: WorkflowConfig,
    run: dict[str, Any],
    jobs: list[dict[str, Any]],
) -> list[CheckRow]:
    jobs_by_name = {job.get("name", ""): job for job in jobs}
    rows: list[CheckRow] = []
    seen_job_names: set[str] = set()

    for job_config in workflow.jobs:
        job = jobs_by_name.get(job_config.job_name)
        if job is None:
            rows.append(CheckRow(job_config.display_name, status_for_missing_job(run), run.get("html_url", "")))
            continue

        seen_job_names.add(job_config.job_name)
        rows.append(row_from_job(client, job_config.display_name, run, job))

    for job in jobs:
        job_name = job.get("name", "")
        if job_name in seen_job_names or not is_failed_job(job):
            continue
        rows.append(row_from_job(client, job_name, run, job))

    return rows


def row_from_job(
    client: GitHubClient,
    display_name: str,
    run: dict[str, Any],
    job: dict[str, Any],
) -> CheckRow:
    status = status_from_job(job, run)
    url = job.get("html_url") or run.get("html_url", "")
    failure = None

    if status == "failure":
        try:
            log_text = client.download_job_log(job["id"])
            failure = extract_failure_details(display_name, log_text)
        except Exception as error:
            failure = FailureDetails(
                error_text=f"Не удалось скачать лог job: {error}",
                test_names=[],
                log_text="",
            )

    return CheckRow(display_name, status, url, failure)


def status_from_job(job: dict[str, Any], run: dict[str, Any]) -> str:
    if job.get("status") != "completed":
        return "in_progress"

    conclusion = job.get("conclusion")
    if conclusion in SUCCESS_CONCLUSIONS:
        return "success"
    if conclusion in FAILURE_CONCLUSIONS:
        return "failure"
    return status_from_run(run)


def status_from_run(run: dict[str, Any]) -> str:
    status = run.get("status")
    conclusion = run.get("conclusion")

    if status != "completed":
        return "in_progress"
    if conclusion in SUCCESS_CONCLUSIONS:
        return "success"
    if conclusion in FAILURE_CONCLUSIONS:
        return "failure"
    return "in_progress"


def status_for_missing_job(run: dict[str, Any]) -> str:
    if run.get("status") != "completed":
        return "in_progress"
    if run.get("conclusion") in SUCCESS_CONCLUSIONS:
        return "success"
    return "skipped"


def is_failed_job(job: dict[str, Any]) -> bool:
    return job.get("status") == "completed" and job.get("conclusion") in FAILURE_CONCLUSIONS


def extract_failure_details(job_name: str, log_text: str) -> FailureDetails:
    lines = log_text.splitlines()
    first_failure = find_first_failure_index(lines)
    start = find_log_start(lines, first_failure)
    end = find_failure_end(lines, first_failure)

    error_text = extract_error_text(lines, first_failure, end)
    if not error_text:
        error_text = f"{job_name} завершился с ошибкой, но явный блок ошибки в логе не найден."

    test_names = extract_failed_test_names(lines)
    log_excerpt = "\n".join(lines[start:end])

    return FailureDetails(
        error_text=truncate(error_text, MAX_ERROR_CHARS),
        test_names=test_names[:20],
        log_text=truncate(log_excerpt, MAX_LOG_CHARS),
    )


def find_first_failure_index(lines: list[str]) -> int:
    patterns = (
        re.compile(r"^\s*Failed\s+[\w.]+"),
        re.compile(r"::error\b"),
        re.compile(r"##\[error\]"),
        re.compile(r"\bFAILED\b"),
        re.compile(r"\bFailed!\s+-\s+Failed:"),
        re.compile(r"^\s*error(?:[:\s]|$)", re.IGNORECASE),
    )

    for index, line in enumerate(lines):
        if any(pattern.search(line) for pattern in patterns):
            return index

    return max(0, len(lines) - 80)


def find_log_start(lines: list[str], first_failure: int) -> int:
    for index in range(first_failure, -1, -1):
        line = lines[index]
        if " Run " in line or line.startswith("Run ") or "dotnet test" in line:
            return index
    return max(0, first_failure - 80)


def find_failure_end(lines: list[str], first_failure: int) -> int:
    if first_failure >= len(lines):
        return len(lines)

    for index in range(first_failure + 1, len(lines)):
        line = lines[index]
        if index - first_failure > 220:
            return index
        if " Run dotnet tool install -g dotnet-trx" in line:
            return index
        if re.search(r"^\d{4}-\d\d-\d\dT.*\s(Post|Run|Complete|Cleanup)\s", line):
            return index

    return len(lines)


def extract_error_text(lines: list[str], first_failure: int, end: int) -> str:
    if not lines:
        return ""

    window = lines[first_failure:end]
    captured: list[str] = []
    include_next = False

    for line in window:
        clean = strip_actions_noise(line)
        if not clean:
            continue

        if include_next:
            captured.append(clean)
            continue

        if re.search(r"^\s*Failed\s+[\w.]+", clean):
            captured.append(clean)
            include_next = True
            continue

        if "Error Message:" in clean or "Stack Trace:" in clean:
            captured.append(clean)
            include_next = True
            continue

        if "::error" in clean or "##[error]" in clean or re.search(r"^\s*error(?:[:\s]|$)", clean, re.IGNORECASE):
            captured.append(clean)

    if not captured and window:
        captured = [strip_actions_noise(line) for line in window[:80] if strip_actions_noise(line)]

    return "\n".join(captured)


def extract_failed_test_names(lines: list[str]) -> list[str]:
    names: list[str] = []
    patterns = (
        re.compile(r"^\s*Failed\s+([\w.`+\-/]+(?:\.[\w.`+\-/]+)+)"),
        re.compile(r"^\s*Failed\s+([\w.`+\-/]+)"),
    )

    for line in lines:
        for pattern in patterns:
            match = pattern.search(strip_actions_noise(line))
            if not match:
                continue
            name = match.group(1).strip()
            if name and name not in names:
                names.append(name)
            break

    return names


def strip_actions_noise(line: str) -> str:
    return re.sub(r"^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d\.\d+Z\s+", "", line).rstrip()


def build_comment(
    pr_number: int,
    head_sha: str,
    rows: list[CheckRow],
    generated_at: str | None = None,
) -> str:
    generated_at = generated_at or datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    short_sha = head_sha[:12]

    lines = [
        COMMENT_MARKER,
        "### Проверки PR",
        "",
        f"PR #{pr_number}, коммит `{short_sha}`. Обновлено: `{generated_at}`.",
        "",
        "| Проверка | Статус | Лог |",
        "| --- | --- | --- |",
    ]

    for row in rows:
        lines.append(f"| {escape_table_cell(row.name)} | {status_icon(row.status)} | {log_link(row.url)} |")

    failed_rows = [row for row in rows if row.status == "failure" and row.failure is not None]
    if failed_rows:
        lines.extend(["", "Ошибки:"])
        body = "\n".join(lines).strip()
        omitted = 0

        for index, row in enumerate(failed_rows):
            block = failure_details_block(row)
            candidate = body + "\n\n" + block

            if len(candidate) > COMMENT_SOFT_LIMIT:
                block = failure_details_block(compact_failure_row(row))
                candidate = body + "\n\n" + block

            if len(candidate) > COMMENT_SOFT_LIMIT:
                omitted = len(failed_rows) - index
                break

            body = candidate

        if omitted:
            body += f"\n\n_Ещё {omitted} падений не поместились в комментарий. Полный вывод доступен по ссылкам в таблице._"

        return trim_comment(body.strip() + "\n")
    else:
        lines.extend(["", "Падений в актуальных запусках пока нет."])

    body = "\n".join(lines).strip() + "\n"
    return trim_comment(body)


def failure_details_block(row: CheckRow) -> str:
    assert row.failure is not None

    parts = [
        "<details>",
        f"<summary>{status_icon(row.status)} {html.escape(row.name)}</summary>",
        "",
    ]

    if row.failure.test_names:
        parts.append("Тесты:")
        for test_name in row.failure.test_names:
            parts.append(f"- `{test_name}`")
        parts.append("")

    parts.extend(
        [
            "Ошибки:",
            "",
            f"<pre>{html.escape(row.failure.error_text)}</pre>",
        ]
    )

    if row.failure.log_text and is_test_row(row):
        parts.extend(
            [
                "",
                "<details>",
                "<summary>Лог от запуска до падения</summary>",
                "",
                f"<pre>{html.escape(row.failure.log_text)}</pre>",
                "</details>",
            ]
        )

    parts.append("</details>")
    return "\n".join(parts)


def compact_failure_row(row: CheckRow) -> CheckRow:
    assert row.failure is not None

    return CheckRow(
        row.name,
        row.status,
        row.url,
        FailureDetails(
            error_text=truncate(row.failure.error_text, 2000),
            test_names=row.failure.test_names[:10],
            log_text=truncate(row.failure.log_text, 3000),
        ),
    )


def is_test_row(row: CheckRow) -> bool:
    return bool(row.failure and row.failure.test_names) or "test" in row.name.lower()


def status_icon(status: str) -> str:
    if status == "success":
        return "✅"
    if status == "failure":
        return "❌"
    if status == "skipped":
        return "➖"
    return "⏳"


def log_link(url: str) -> str:
    if not url:
        return "-"
    return f"[лог]({url})"


def escape_table_cell(value: str) -> str:
    return value.replace("|", "\\|").replace("\n", " ")


def trim_comment(body: str) -> str:
    if len(body) <= COMMENT_SOFT_LIMIT:
        return body

    trimmed = body[: COMMENT_SOFT_LIMIT - 2000]
    return (
        trimmed
        + "\n\n_Комментарий был обрезан, потому что GitHub ограничивает размер сообщения. "
        + "Открой логи job по ссылкам в таблице, чтобы увидеть полный вывод._\n"
    )


def truncate(value: str, limit: int) -> str:
    if len(value) <= limit:
        return value
    return value[:limit] + "\n... <обрезано>"


def upsert_comment(client: GitHubClient, pr_number: int, body: str) -> None:
    comments = client.paginate(f"/repos/{client.repository}/issues/{pr_number}/comments")
    existing = next((comment for comment in comments if COMMENT_MARKER in (comment.get("body") or "")), None)

    if existing:
        client.request(
            "PATCH",
            f"/repos/{client.repository}/issues/comments/{existing['id']}",
            body={"body": body},
        )
        return

    client.request(
        "POST",
        f"/repos/{client.repository}/issues/{pr_number}/comments",
        body={"body": body},
    )


if __name__ == "__main__":
    raise SystemExit(main())
