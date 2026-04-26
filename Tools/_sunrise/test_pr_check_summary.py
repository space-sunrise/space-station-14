import unittest
import importlib.util
from pathlib import Path

MODULE_PATH = Path(__file__).with_name("pr_check_summary.py")
SPEC = importlib.util.spec_from_file_location("pr_check_summary", MODULE_PATH)
pr_check_summary = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(pr_check_summary)

CheckRow = pr_check_summary.CheckRow
FailureDetails = pr_check_summary.FailureDetails
build_comment = pr_check_summary.build_comment
extract_failure_details = pr_check_summary.extract_failure_details
latest_tracked_runs = pr_check_summary.latest_tracked_runs
get_jobs_for_run = pr_check_summary.get_jobs_for_run
rows_for_workflow = pr_check_summary.rows_for_workflow
WorkflowConfig = pr_check_summary.WorkflowConfig
JobConfig = pr_check_summary.JobConfig


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

    def test_missing_dependent_job_stays_pending_when_workflow_failed_earlier(self):
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

        class FakeClient:
            def download_job_log(self, job_id):
                return "Run dotnet build\n##[error]Build failed"

        rows = rows_for_workflow(FakeClient(), workflow, run, jobs)

        self.assertEqual("failure", rows[0].status)
        self.assertEqual("in_progress", rows[1].status)
        self.assertIsNone(rows[1].failure)


if __name__ == "__main__":
    unittest.main()
