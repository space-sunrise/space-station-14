import unittest
import importlib.util
import io
import json
import os
import sys
import tempfile
import urllib.request
import zipfile
from pathlib import Path
from unittest import mock

MODULE_PATH = Path(__file__).with_name("pr_check_summary.py")
SPEC = importlib.util.spec_from_file_location("pr_check_summary", MODULE_PATH)
pr_check_summary = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(pr_check_summary)

CheckRow = pr_check_summary.CheckRow
FailureDetails = pr_check_summary.FailureDetails
build_comment = pr_check_summary.build_comment
find_first_failure_index = pr_check_summary.find_first_failure_index
extract_failure_details = pr_check_summary.extract_failure_details
extract_error_text = pr_check_summary.extract_error_text
latest_tracked_runs = pr_check_summary.latest_tracked_runs
get_jobs_for_run = pr_check_summary.get_jobs_for_run
rows_for_workflow = pr_check_summary.rows_for_workflow
resolve_pull_request = pr_check_summary.resolve_pull_request
status_for_missing_job = pr_check_summary.status_for_missing_job
status_from_job = pr_check_summary.status_from_job
status_from_run = pr_check_summary.status_from_run
upsert_comment = pr_check_summary.upsert_comment
GitHubClient = pr_check_summary.GitHubClient
WorkflowConfig = pr_check_summary.WorkflowConfig
JobConfig = pr_check_summary.JobConfig


class FakeResponse:
    def __init__(self, data: bytes):
        self.data = data

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc, traceback):
        return False

    def read(self):
        return self.data


class RecordingClient:
    repository = "space-sunrise/sunrise-station"

    def __init__(self, *, comments=None, responses=None):
        self.comments = comments or []
        self.responses = list(responses or [])
        self.paginate_calls = []
        self.requests = []

    def paginate(self, path, *, params=None):
        self.paginate_calls.append((path, params))
        return self.comments

    def request(self, method, path, **kwargs):
        self.requests.append((method, path, kwargs))
        if self.responses:
            response = self.responses.pop(0)
            if isinstance(response, Exception):
                raise response
            return response
        return None


class PrCheckSummaryTests(unittest.TestCase):
    def test_build_comment_renders_status_table_and_failure_details(self):
        rows = [
            CheckRow("YAML Linter", "success", "https://example.test/yaml"),
            CheckRow(
                "Integration Tests (shard 3)",
                "failure",
                "https://example.test/shard-3",
                FailureDetails(
                    error_text="Expected entity to be deleted.",
                    test_names=["Content.IntegrationTests.DeleteEntityTest"],
                    log_text="Run Content.IntegrationTests\nExpected entity to be deleted.",
                ),
            ),
        ]

        body = build_comment(
            pr_number=42,
            head_sha="abc123def456",
            rows=rows,
            generated_at="2026-04-26T10:20:30Z",
        )

        self.assertIn("<!-- sunrise-pr-check-summary -->", body)
        self.assertIn("| YAML Linter | ✅ | [лог](https://example.test/yaml) |", body)
        self.assertIn("| Integration Tests (shard 3) | ❌ | [лог](https://example.test/shard-3) |", body)
        self.assertIn("<summary>❌ Integration Tests (shard 3)</summary>", body)
        self.assertIn("Content.IntegrationTests.DeleteEntityTest", body)
        self.assertIn("Expected entity to be deleted.", body)
        self.assertIn("<summary>Лог от запуска до падения</summary>", body)

    def test_build_comment_omits_failure_details_for_restarted_pending_job(self):
        rows = [
            CheckRow("Content Tests", "in_progress", "https://example.test/content"),
        ]

        body = build_comment(
            pr_number=42,
            head_sha="abc123def456",
            rows=rows,
            generated_at="2026-04-26T10:20:30Z",
        )

        self.assertIn("| Content Tests | ⏳ | [лог](https://example.test/content) |", body)
        self.assertNotIn("<summary>❌ Content Tests</summary>", body)
        self.assertNotIn("OLD ERROR", body)

    def test_extract_failure_details_finds_failed_test_name_error_and_early_log(self):
        log_text = "\n".join(
            [
                "2026-04-26T10:00:00.000Z Run Content.IntegrationTests",
                "Some warmup line",
                "Failed Content.IntegrationTests.DeleteEntityTest [123 ms]",
                "Error Message:",
                " Expected entity to be deleted.",
                "Stack Trace:",
                " at Content.IntegrationTests.DeleteEntityTest() in Tests.cs:line 10",
                "Failed!  - Failed: 1, Passed: 20, Skipped: 0",
                "2026-04-26T10:01:00.000Z Run dotnet tool install -g dotnet-trx",
            ]
        )

        details = extract_failure_details("Integration Tests (shard 3)", log_text)

        self.assertIn("Content.IntegrationTests.DeleteEntityTest", details.test_names)
        self.assertIn("Expected entity to be deleted.", details.error_text)
        self.assertIn("Run Content.IntegrationTests", details.log_text)
        self.assertIn("Stack Trace:", details.log_text)
        self.assertNotIn("dotnet-trx", details.log_text)

    def test_latest_run_prefers_new_in_progress_rerun_over_old_failure(self):
        runs = [
            {
                "id": 100,
                "name": "Build & Test Debug",
                "event": "pull_request",
                "created_at": "2026-04-26T10:00:00Z",
                "run_attempt": 1,
                "status": "completed",
                "conclusion": "failure",
            },
            {
                "id": 100,
                "name": "Build & Test Debug",
                "event": "pull_request",
                "created_at": "2026-04-26T10:00:00Z",
                "run_attempt": 2,
                "status": "in_progress",
                "conclusion": None,
            },
        ]

        latest = latest_tracked_runs(runs)

        self.assertEqual(2, latest["Build & Test Debug"]["run_attempt"])
        self.assertEqual("in_progress", latest["Build & Test Debug"]["status"])

    def test_job_collection_uses_latest_job_filter_for_reruns(self):
        class FakeClient:
            repository = "space-sunrise/sunrise-station"

            def __init__(self):
                self.path = None
                self.params = None

            def paginate(self, path, *, params=None):
                self.path = path
                self.params = params
                return []

        client = FakeClient()

        jobs = get_jobs_for_run(client, {"id": 123})

        self.assertEqual([], jobs)
        self.assertEqual("/repos/space-sunrise/sunrise-station/actions/runs/123/jobs", client.path)
        self.assertEqual({"filter": "latest"}, client.params)

    def test_missing_dependent_job_is_marked_skipped_when_workflow_failed_earlier(self):
        workflow = WorkflowConfig(
            "Build & Test Debug",
            (
                JobConfig("build", "Build"),
                JobConfig("Content Tests", "Content Tests"),
            ),
        )
        run = {
            "status": "completed",
            "conclusion": "failure",
            "html_url": "https://example.test/run",
        }
        jobs = [
            {
                "name": "build",
                "status": "completed",
                "conclusion": "failure",
                "html_url": "https://example.test/build",
                "id": 1,
            }
        ]

        client = mock.MagicMock(spec=GitHubClient)
        client.download_job_log.return_value = "Run dotnet build\n##[error]Build failed"

        rows = rows_for_workflow(client, workflow, run, jobs)

        self.assertEqual("failure", rows[0].status)
        self.assertEqual("skipped", rows[1].status)
        self.assertIsNone(rows[1].failure)

    def test_upsert_comment_patches_existing_marker_comment(self):
        client = RecordingClient(
            comments=[
                {"id": 10, "body": "plain comment"},
                {"id": 20, "body": f"old\n{pr_check_summary.COMMENT_MARKER}"},
            ]
        )

        upsert_comment(client, 42, "new body")

        self.assertEqual(
            [("/repos/space-sunrise/sunrise-station/issues/42/comments", None)],
            client.paginate_calls,
        )
        self.assertEqual(
            [
                (
                    "PATCH",
                    "/repos/space-sunrise/sunrise-station/issues/comments/20",
                    {"body": {"body": "new body"}},
                )
            ],
            client.requests,
        )

    def test_upsert_comment_posts_when_marker_comment_is_missing(self):
        client = RecordingClient(comments=[{"id": 10, "body": "plain comment"}])

        upsert_comment(client, 42, "new body")

        self.assertEqual(
            [
                (
                    "POST",
                    "/repos/space-sunrise/sunrise-station/issues/42/comments",
                    {"body": {"body": "new body"}},
                )
            ],
            client.requests,
        )

    def test_resolve_pull_request_uses_pull_request_payload_number(self):
        client = RecordingClient(
            responses=[
                {"number": 42, "state": "open", "head": {"sha": "abc123"}},
            ]
        )

        pull_request = resolve_pull_request(
            client,
            {"pull_request": {"number": 42}},
            "pull_request",
            explicit_pr_number=None,
        )

        self.assertEqual(42, pull_request["number"])
        self.assertEqual("GET", client.requests[0][0])
        self.assertEqual("/repos/space-sunrise/sunrise-station/pulls/42", client.requests[0][1])

    def test_resolve_pull_request_uses_pull_request_target_payload_number(self):
        client = RecordingClient(
            responses=[
                {"number": 77, "state": "open", "head": {"sha": "def456"}},
            ]
        )

        pull_request = resolve_pull_request(
            client,
            {"pull_request": {"number": 77}},
            "pull_request_target",
            explicit_pr_number=None,
        )

        self.assertEqual(77, pull_request["number"])

    def test_resolve_pull_request_uses_workflow_run_pull_request_number(self):
        client = RecordingClient(
            responses=[
                {"number": 42, "state": "open", "head": {"sha": "abc123"}},
            ]
        )
        payload = {
            "workflow_run": {
                "event": "pull_request",
                "head_sha": "abc123",
                "pull_requests": [{"number": 42}],
            }
        }

        pull_request = resolve_pull_request(client, payload, "workflow_run", explicit_pr_number=None)

        self.assertEqual(42, pull_request["number"])

    def test_resolve_pull_request_finds_fork_pr_by_workflow_run_head_sha(self):
        client = RecordingClient(
            responses=[
                [{"number": 55, "state": "open"}],
                {"number": 55, "state": "open", "head": {"sha": "forksha"}},
            ]
        )
        payload = {
            "workflow_run": {
                "event": "pull_request",
                "head_sha": "forksha",
                "pull_requests": [],
            }
        }

        pull_request = resolve_pull_request(client, payload, "workflow_run", explicit_pr_number=None)

        self.assertEqual(55, pull_request["number"])
        self.assertEqual(
            "/repos/space-sunrise/sunrise-station/commits/forksha/pulls",
            client.requests[0][1],
        )

    def test_resolve_pull_request_skips_workflow_run_without_pull_request_context(self):
        client = RecordingClient()

        pull_request = resolve_pull_request(
            client,
            {"workflow_run": {"event": "push", "head_sha": "abc123", "pull_requests": []}},
            "workflow_run",
            explicit_pr_number=None,
        )

        self.assertIsNone(pull_request)
        self.assertEqual([], client.requests)

    def test_resolve_pull_request_skips_workflow_run_without_prs_or_head_sha(self):
        client = RecordingClient()

        pull_request = resolve_pull_request(
            client,
            {"workflow_run": {"event": "pull_request", "pull_requests": []}},
            "workflow_run",
            explicit_pr_number=None,
        )

        self.assertIsNone(pull_request)
        self.assertEqual([], client.requests)

    def test_resolve_pull_request_skips_closed_pr(self):
        client = RecordingClient(
            responses=[
                {"number": 42, "state": "closed", "head": {"sha": "abc123"}},
            ]
        )

        pull_request = resolve_pull_request(
            client,
            {"pull_request": {"number": 42}},
            "pull_request",
            explicit_pr_number=None,
        )

        self.assertIsNone(pull_request)

    def test_resolve_pull_request_skips_stale_workflow_run_sha(self):
        client = RecordingClient(
            responses=[
                {"number": 42, "state": "open", "head": {"sha": "newsha"}},
            ]
        )
        payload = {
            "workflow_run": {
                "event": "pull_request",
                "head_sha": "oldsha",
                "pull_requests": [{"number": 42}],
            }
        }

        pull_request = resolve_pull_request(client, payload, "workflow_run", explicit_pr_number=None)

        self.assertIsNone(pull_request)

    def test_download_job_log_uses_default_urlopen_and_extracts_zip_logs(self):
        buffer = io.BytesIO()
        with zipfile.ZipFile(buffer, "w") as archive:
            archive.writestr("1_build.txt", "first log")
            archive.writestr("2_test.txt", "second log")

        with mock.patch.object(
            pr_check_summary.urllib.request,
            "urlopen",
            return_value=FakeResponse(buffer.getvalue()),
        ) as urlopen:
            text = GitHubClient("token", "space-sunrise/sunrise-station").download_job_log(123)

        self.assertEqual("first log\nsecond log", text)
        request = urlopen.call_args.args[0]
        self.assertIsInstance(request, urllib.request.Request)
        self.assertEqual(
            "https://api.github.com/repos/space-sunrise/sunrise-station/actions/jobs/123/logs",
            request.full_url,
        )

    def test_download_job_log_decodes_plain_text_logs(self):
        with mock.patch.object(
            pr_check_summary.urllib.request,
            "urlopen",
            return_value=FakeResponse(b"first log\nsecond log"),
        ):
            text = GitHubClient("token", "space-sunrise/sunrise-station").download_job_log(123)

        self.assertEqual("first log\nsecond log", text)

    def test_status_helpers_cover_run_job_and_missing_job_states(self):
        run_cases = [
            ({"status": "queued", "conclusion": None}, "in_progress"),
            ({"status": "completed", "conclusion": "success"}, "success"),
            ({"status": "completed", "conclusion": "failure"}, "failure"),
            ({"status": "completed", "conclusion": "unknown"}, "in_progress"),
        ]
        for run, expected in run_cases:
            with self.subTest(run=run):
                self.assertEqual(expected, status_from_run(run))

        job_cases = [
            ({"status": "queued", "conclusion": None}, {"status": "completed", "conclusion": "success"}, "in_progress"),
            ({"status": "completed", "conclusion": "skipped"}, {"status": "completed", "conclusion": "failure"}, "success"),
            ({"status": "completed", "conclusion": "failure"}, {"status": "completed", "conclusion": "success"}, "failure"),
            ({"status": "completed", "conclusion": "unknown"}, {"status": "completed", "conclusion": "failure"}, "failure"),
        ]
        for job, run, expected in job_cases:
            with self.subTest(job=job, run=run):
                self.assertEqual(expected, status_from_job(job, run))

        missing_cases = [
            ({"status": "queued", "conclusion": None}, "in_progress"),
            ({"status": "completed", "conclusion": "success"}, "success"),
            ({"status": "completed", "conclusion": "failure"}, "skipped"),
        ]
        for run, expected in missing_cases:
            with self.subTest(missing_run=run):
                self.assertEqual(expected, status_for_missing_job(run))

    def test_find_first_failure_index_ignores_mid_line_error_noise(self):
        lines = [
            "Dependency restore completed without error count changes.",
            "Build still running.",
            "##[error]Actual failure",
        ]

        self.assertEqual(2, find_first_failure_index(lines))

    def test_extract_error_text_prefers_explicit_markers_over_mid_line_error_noise(self):
        lines = [
            "Restored without error.",
            "Build still running.",
            "##[error]Actual compiler error",
        ]

        text = extract_error_text(lines, 0, len(lines))

        self.assertEqual("##[error]Actual compiler error", text)

    def test_extract_error_text_fallback_requires_error_prefix(self):
        lines = [
            "Restored without error.",
            "Build still running.",
            "error: Actual compiler error",
        ]

        text = extract_error_text(lines, 0, len(lines))

        self.assertEqual("error: Actual compiler error", text)

    def test_paginate_follows_link_header_next_url(self):
        case = self

        class PaginatedClient(GitHubClient):
            def __init__(self):
                super().__init__("token", "space-sunrise/sunrise-station")
                self.calls = []

            def request(self, method, path, **kwargs):
                self.calls.append((method, path, kwargs))
                case.assertTrue(kwargs.get("return_headers"))
                if len(self.calls) == 1:
                    return (
                        {"workflow_runs": [{"id": 1}]},
                        {"Link": '<https://api.github.com/repos/space-sunrise/sunrise-station/actions/runs?page=2>; rel="next"'},
                    )
                return ({"workflow_runs": [{"id": 2}]}, {})

        client = PaginatedClient()

        runs = client.paginate("/repos/space-sunrise/sunrise-station/actions/runs")

        self.assertEqual([{"id": 1}, {"id": 2}], runs)
        self.assertEqual(
            "https://api.github.com/repos/space-sunrise/sunrise-station/actions/runs?page=2",
            client.calls[1][1],
        )
        self.assertIsNone(client.calls[1][2]["params"])

    def test_paginate_fails_loudly_for_unknown_response_collection_key(self):
        class UnknownCollectionClient(GitHubClient):
            def __init__(self):
                super().__init__("token", "space-sunrise/sunrise-station")

            def request(self, method, path, **kwargs):
                return ({"unexpected": []}, {})

        with self.assertRaisesRegex(RuntimeError, "paginated response"):
            UnknownCollectionClient().paginate("/repos/space-sunrise/sunrise-station/unknown")

    def test_main_prints_runtime_error_and_keeps_workflow_green(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            event_path = Path(temp_dir) / "event.json"
            event_path.write_text(json.dumps({"pull_request": {"number": 42}}), encoding="utf-8")

            with (
                mock.patch.dict(
                    os.environ,
                    {
                        "GITHUB_TOKEN": "token",
                        "GITHUB_REPOSITORY": "space-sunrise/sunrise-station",
                        "GITHUB_EVENT_NAME": "pull_request",
                        "GITHUB_EVENT_PATH": str(event_path),
                    },
                    clear=False,
                ),
                mock.patch.object(sys, "argv", ["pr_check_summary.py"]),
                mock.patch.object(
                    pr_check_summary,
                    "resolve_pull_request",
                    side_effect=RuntimeError("GitHub API exploded"),
                ),
                mock.patch("sys.stderr", new_callable=io.StringIO) as stderr,
            ):
                exit_code = pr_check_summary.main()

        self.assertEqual(0, exit_code)
        self.assertIn("GitHub API exploded", stderr.getvalue())

    def test_build_comment_trims_failed_rows_branch(self):
        rows = [
            CheckRow(f"Successful check {index}", "success", f"https://example.test/{index}")
            for index in range(300)
        ]
        rows.append(
            CheckRow(
                "Content Tests",
                "failure",
                "https://example.test/content",
                FailureDetails(
                    error_text="boom",
                    test_names=["Content.Tests.BrokenTest"],
                    log_text="Run Content.Tests\n##[error]boom",
                ),
            )
        )

        with mock.patch.object(pr_check_summary, "COMMENT_SOFT_LIMIT", 3000):
            body = build_comment(
                pr_number=42,
                head_sha="abc123def456",
                rows=rows,
                generated_at="2026-04-26T10:20:30Z",
            )

        self.assertLessEqual(len(body), 3000)
        self.assertIn("Комментарий был обрезан", body)


if __name__ == "__main__":
    unittest.main()
