#!/usr/bin/env python3

import argparse
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any
from urllib.error import HTTPError
from urllib.parse import urlencode
from urllib.request import Request, urlopen

import yaml

from changelog_path import validate_changelog_path


MAIN_CATEGORY = "Main"


def configured_changelog_file() -> Path:
    return validate_changelog_path(os.environ.get("CHANGELOG_FILE"))


CHANGELOG_FILE = configured_changelog_file()
CATEGORY_FILES = {
    MAIN_CATEGORY: CHANGELOG_FILE.name,
}
WORKFLOW_FILE = "changelog.yml"
PARTS_PATH = Path("Resources/Changelog/Parts")
CHANGELOG_PATH = CHANGELOG_FILE.parent

COMMENT_RE = re.compile(r"(?<!\\)<!--([^>]+)(?<!\\)-->")
MARKER_RE = re.compile(r"^\s*(?::cl:|🆑)", re.IGNORECASE | re.MULTILINE)
HEADER_RE = re.compile(
    r"^\s*(?::cl:|🆑) *([a-z0-9_\- ,&]+)?\s*$",
    re.IGNORECASE | re.MULTILINE,
)
ENTRY_RE = re.compile(
    r"^ *[*-]? *(add|remove|tweak|fix|bug|bugfix): *([^\n\r]+)\r?$",
    re.IGNORECASE,
)
MALFORMED_ENTRY_RE = re.compile(r"^ *[*-] *(?:[a-z]+):", re.IGNORECASE)
CATEGORY_RE = re.compile(r"^\s*([a-z]+):\s*$", re.IGNORECASE)
CHANGE_TYPES = {
    "add": "Add",
    "remove": "Remove",
    "tweak": "Tweak",
    "fix": "Fix",
    "bug": "Fix",
    "bugfix": "Fix",
}
STATUS_FORMAT = {
    "success": ("notice", "✅"),
    "skip": ("notice", "⏭️"),
    "error": ("error", "❌"),
}


@dataclass(frozen=True)
class ParsedCategory:
    name: str
    changes: list[dict[str, str]]


def report_status(status: str, message: str) -> None:
    command, icon = STATUS_FORMAT[status]
    escaped = message.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")
    print(f"::{command}::{escaped}")

    if summary_path := os.environ.get("GITHUB_STEP_SUMMARY"):
        summary_file = Path(summary_path)
        is_empty = not summary_file.exists() or summary_file.stat().st_size == 0
        with summary_file.open("a", encoding="utf-8") as summary:
            if is_empty:
                summary.write("## Автоматический чейнджлог\n\n")
            summary.write(f"- {icon} {message}\n")


def parse_time(value: str) -> datetime:
    date, separator, remainder = value.partition("T")
    if separator:
        year, month, day = date.split("-")
        value = f"{int(year):04d}-{int(month):02d}-{int(day):02d}T{remainder}"
    return datetime.fromisoformat(value.replace("Z", "+00:00")).astimezone(timezone.utc)


def format_changelog_time(value: str | None) -> str:
    timestamp = parse_time(value) if value else datetime.now(timezone.utc)
    return timestamp.strftime("%Y-%m-%dT%H:%M:%S.") + f"{timestamp.microsecond:06d}0+00:00"


def parse_pr_body(
    body: str | None,
    fallback_author: str,
    category_names: tuple[str, ...] = tuple(CATEGORY_FILES),
) -> tuple[str, list[ParsedCategory]] | None:
    text = COMMENT_RE.sub("", body or "")
    header = HEADER_RE.search(text)
    if header is None:
        if MARKER_RE.search(text):
            raise ValueError("маркер чейнжлога найден, но его заголовок не удалось распознать")
        return None

    author = header.group(1).strip() if header.group(1) else fallback_author
    current_category = MAIN_CATEGORY
    entries: dict[str, list[dict[str, str]]] = {}

    for line in text[header.end():].splitlines():
        category_match = CATEGORY_RE.match(line)
        if category_match:
            requested = category_match.group(1)
            matched = next((name for name in category_names if name.casefold() == requested.casefold()), None)
            if matched is not None:
                current_category = matched
            continue

        entry_match = ENTRY_RE.match(line)
        if entry_match is None:
            if MALFORMED_ENTRY_RE.match(line):
                raise ValueError(f"не удалось распознать строку чейнжлога: {line.strip()[:120]}")
            continue

        change_type = CHANGE_TYPES[entry_match.group(1).lower()]
        entries.setdefault(current_category, []).append(
            {"type": change_type, "message": entry_match.group(2).strip()},
        )

    return author, [ParsedCategory(name, changes) for name, changes in entries.items()]


def github_request(
    path: str,
    query: dict[str, str | int] | None = None,
    token_environment: str = "GITHUB_TOKEN",
) -> Any:
    api_url = os.environ.get("GITHUB_API_URL", "https://api.github.com").rstrip("/")
    url = f"{api_url}{path}"
    if query:
        url += "?" + urlencode(query)

    headers = {
        "Accept": "application/vnd.github+json",
        "User-Agent": "Sunrise-Changelog-Actions",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    if token := os.environ.get(token_environment):
        headers["Authorization"] = f"Bearer {token}"

    try:
        with urlopen(Request(url, headers=headers), timeout=30) as response:
            return json.load(response)
    except HTTPError as error:
        details = error.read().decode("utf-8", errors="replace")[:500]
        raise RuntimeError(f"GitHub API вернул {error.code} для {path}: {details}") from error


def repository_slug() -> str:
    slug = os.environ.get("GITHUB_REPOSITORY", "")
    if slug.count("/") != 1:
        raise RuntimeError("GITHUB_REPOSITORY должен иметь вид owner/repository")
    return slug


def load_event_pull_request(event_path: Path) -> dict[str, Any] | None:
    event = json.loads(event_path.read_text(encoding="utf-8"))
    pull_request = event.get("pull_request")
    return pull_request if isinstance(pull_request, dict) else None


def load_pull_request(number: int) -> dict[str, Any]:
    return github_request(f"/repos/{repository_slug()}/pulls/{number}")


def latest_changelog_time(repo_root: Path, category_files: dict[str, str] = CATEGORY_FILES) -> datetime:
    latest = datetime(1970, 1, 1, tzinfo=timezone.utc)
    for filename in category_files.values():
        path = repo_root / CHANGELOG_PATH / filename
        if not path.exists():
            continue

        document = yaml.safe_load(path.read_text(encoding="utf-8-sig")) or {}
        for entry in document.get("Entries", []):
            if value := entry.get("time"):
                latest = max(latest, parse_time(str(value)))
    return latest


def load_checkpoint(repo_root: Path, category_files: dict[str, str] = CATEGORY_FILES) -> datetime:
    response = github_request(
        f"/repos/{repository_slug()}/actions/workflows/{WORKFLOW_FILE}/runs",
        {"status": "success", "per_page": 100},
        token_environment="ACTIONS_TOKEN",
    )
    current_run_id = os.environ.get("GITHUB_RUN_ID")
    previous_runs = sorted(
        [
            run
            for run in response.get("workflow_runs", [])
            if str(run.get("id")) != current_run_id
        ],
        key=lambda run: parse_time(run["created_at"]),
        reverse=True,
    )
    for previous_run in previous_runs:
        jobs = github_request(
            f"/repos/{repository_slug()}/actions/runs/{previous_run['id']}/jobs",
            {"per_page": 100},
            token_environment="ACTIONS_TOKEN",
        )
        if any(
            job.get("name") == "update" and job.get("conclusion") == "success"
            for job in jobs.get("jobs", [])
        ):
            return parse_time(previous_run.get("run_started_at") or previous_run["created_at"])

    report_status(
        "skip",
        "Предыдущий успешный запуск Actions не найден: сверяем от последней записи чейнжлога.",
    )
    return latest_changelog_time(repo_root, category_files)


def list_merged_pull_requests(checkpoint: datetime) -> list[dict[str, Any]]:
    pulls: list[dict[str, Any]] = []
    page = 1

    while True:
        page_items = github_request(
            f"/repos/{repository_slug()}/pulls",
            {
                "state": "closed",
                "sort": "updated",
                "direction": "desc",
                "per_page": 100,
                "page": page,
            },
        )
        if not page_items:
            break

        reached_checkpoint = False
        for pull_request in page_items:
            updated_at = parse_time(pull_request["updated_at"])
            if updated_at < checkpoint:
                reached_checkpoint = True

            merged_at = pull_request.get("merged_at")
            if merged_at and parse_time(merged_at) >= checkpoint:
                pulls.append(pull_request)

        if reached_checkpoint or len(page_items) < 100:
            break
        page += 1

    return pulls


def load_known_urls(
    repo_root: Path,
    category_files: dict[str, str] = CATEGORY_FILES,
) -> dict[str, set[str]]:
    known = {category: set() for category in category_files}

    for category, filename in category_files.items():
        path = repo_root / CHANGELOG_PATH / filename
        if not path.exists():
            continue
        document = yaml.safe_load(path.read_text(encoding="utf-8-sig")) or {}
        known[category].update(
            str(entry["url"])
            for entry in document.get("Entries", [])
            if entry.get("url")
        )

    parts_dir = repo_root / PARTS_PATH
    for path in parts_dir.glob("*.yml"):
        part = yaml.safe_load(path.read_text(encoding="utf-8-sig")) or {}
        category = part.get("category", MAIN_CATEGORY)
        if category in known and part.get("url"):
            known[category].add(str(part["url"]))

    return known


def is_target_pull_request(pull_request: dict[str, Any], target_branch: str) -> bool:
    return bool(
        pull_request.get("merged_at")
        and pull_request.get("base", {}).get("ref") == target_branch
        and pull_request.get("merged", True)
    )


def write_pull_request_parts(
    repo_root: Path,
    pull_requests: list[dict[str, Any]],
    target_branch: str,
    category_files: dict[str, str] = CATEGORY_FILES,
) -> int:
    known_urls = load_known_urls(repo_root, category_files)
    written = 0

    for pull_request in sorted(pull_requests, key=lambda item: item.get("merged_at") or ""):
        number = int(pull_request["number"])
        if not is_target_pull_request(pull_request, target_branch):
            report_status("skip", f"PR #{number} пропущен: он не был слит в ветку {target_branch}.")
            continue

        try:
            parsed = parse_pr_body(
                pull_request.get("body"),
                pull_request.get("user", {}).get("login", "unknown"),
                tuple(category_files),
            )
        except ValueError as error:
            raise RuntimeError(f"PR #{number}: {error}") from error
        if parsed is None:
            report_status("skip", f"PR #{number} пропущен: отсутствует маркер :cl: или 🆑.")
            continue

        author, categories = parsed
        if not categories:
            raise RuntimeError(
                f"PR #{number}: маркер чейнжлога найден, но ни одну запись изменений распознать не удалось.",
            )

        url = str(pull_request["html_url"])
        merged_at = format_changelog_time(pull_request.get("merged_at"))
        pull_request_written = 0

        for category in categories:
            if url in known_urls[category.name]:
                continue

            part = {
                "author": author,
                "time": merged_at,
                "url": url,
                "changes": category.changes,
            }
            if category.name != MAIN_CATEGORY:
                part["category"] = category.name

            path = repo_root / PARTS_PATH / f"pr-{number}-{category.name}.yml"
            path.write_text(
                yaml.safe_dump(part, sort_keys=False, allow_unicode=True),
                encoding="utf-8",
            )
            known_urls[category.name].add(url)
            written += 1
            pull_request_written += 1

        if pull_request_written:
            report_status("success", f"PR #{number} обработан: подготовлено фрагментов — {pull_request_written}.")
        else:
            report_status("skip", f"PR #{number} пропущен: его записи уже присутствуют в чейнжлоге.")

    return written


def update_changelogs(repo_root: Path, category_files: dict[str, str] = CATEGORY_FILES) -> None:
    updater = repo_root / "Tools/update_changelog.py"
    parts = repo_root / PARTS_PATH

    for category, filename in category_files.items():
        command = [
            sys.executable,
            str(updater),
            str(repo_root / CHANGELOG_PATH / filename),
            str(parts),
        ]
        if category != MAIN_CATEGORY:
            command.extend(["--category", category])
        subprocess.run(command, check=True)


def main() -> None:
    parser = argparse.ArgumentParser(description="Обновляет чейнджлог Sunrise из событий GitHub Actions.")
    parser.add_argument("--event-path", type=Path, required=True)
    parser.add_argument("--pr-number", type=int)
    parser.add_argument("--target-branch", default="master")
    parser.add_argument("--extra-category", action="append", default=[])
    args = parser.parse_args()

    repo_root = Path(__file__).resolve().parents[3]
    if not (repo_root / CHANGELOG_FILE).is_file():
        raise RuntimeError(f"Файл чейнжлога не найден: {CHANGELOG_FILE}")

    category_files = CATEGORY_FILES.copy()
    configured_categories = [
        category.strip()
        for category in os.environ.get("CHANGELOG_EXTRA_CATEGORIES", "").split(",")
        if category.strip()
    ]
    configured_categories.extend(args.extra_category)
    for category in configured_categories:
        if re.fullmatch(r"[A-Za-z]+", category) is None:
            raise RuntimeError(f"Недопустимое имя категории чейнджлога: {category}")
        filename = f"{category}.yml"
        if not (repo_root / CHANGELOG_PATH / filename).exists():
            raise RuntimeError(f"Для категории {category} отсутствует {filename}")
        category_files[category] = filename

    checkpoint = load_checkpoint(repo_root, category_files)
    pull_requests = list_merged_pull_requests(checkpoint)

    explicit = load_pull_request(args.pr_number) if args.pr_number else load_event_pull_request(args.event_path)
    by_number = {int(item["number"]): item for item in pull_requests}
    if explicit is not None:
        by_number[int(explicit["number"])] = explicit

    written = write_pull_request_parts(
        repo_root,
        list(by_number.values()),
        args.target_branch,
        category_files,
    )
    update_changelogs(repo_root, category_files)

    if written:
        report_status("success", f"Чейнджлог обновлён: подготовлено фрагментов — {written}.")
    else:
        report_status("success", "Сверка завершена: новых фрагментов для чейнжлога нет.")


def run() -> None:
    try:
        main()
    except Exception as error:
        report_status("error", f"Чейнджлог завершился с ошибкой: {error}")
        raise


if __name__ == "__main__":
    run()
