import io
import subprocess
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from datetime import datetime, timezone
from pathlib import Path
from unittest.mock import Mock, patch

import yaml

import actions_changelogs_since_last_run as discord_changelog
import changelog_actions
import manual_changelog


REPO_ROOT = Path(__file__).resolve().parents[3]
WORKFLOW_PATH = REPO_ROOT / ".github/workflows/changelog.yml"
RUNNER_PATH = REPO_ROOT / "Tools/_sunrise/changelog/run.sh"


class ChangelogActionsTests(unittest.TestCase):
    def test_legacy_non_padded_changelog_time_is_supported(self):
        self.assertEqual(
            datetime(2024, 1, 4, 1, 30, tzinfo=timezone.utc),
            changelog_actions.parse_time("2024-1-4T01:30:00.0000000+00:00"),
        )

    def test_parser_matches_upstream_syntax(self):
        body = """
            <!-- :cl: из шаблона не должен учитываться -->
            :cl: Ev1__l P-JB2323, Other & Co
            - add: Added a thing
            * bugfix: Fixed a thing
            ADMIN:
            tweak: Tweaked a thing
            MAIN:
            bug: Fixed another thing
        """

        author, categories = changelog_actions.parse_pr_body(body, "Fallback", ("Main", "Admin"))

        self.assertEqual("Ev1__l P-JB2323, Other & Co", author)
        self.assertEqual(
            [
                changelog_actions.ParsedCategory(
                    "Main",
                    [
                        {"type": "Add", "message": "Added a thing"},
                        {"type": "Fix", "message": "Fixed a thing"},
                        {"type": "Fix", "message": "Fixed another thing"},
                    ],
                ),
                changelog_actions.ParsedCategory(
                    "Admin",
                    [{"type": "Tweak", "message": "Tweaked a thing"}],
                ),
            ],
            categories,
        )

    def test_parser_uses_fallback_author_and_keeps_category_after_invalid_directive(self):
        parsed = changelog_actions.parse_pr_body(
            ":cl:\nADMIN:\n- remove: First\nNOTACATEGORY:\n- fix: Second",
            "Fallback",
            ("Main", "Admin"),
        )

        self.assertEqual(
            (
                "Fallback",
                [
                    changelog_actions.ParsedCategory(
                        "Admin",
                        [
                            {"type": "Remove", "message": "First"},
                            {"type": "Fix", "message": "Second"},
                        ],
                    ),
                ],
            ),
            parsed,
        )

    def test_pull_request_without_marker_is_logged_as_skip(self):
        with tempfile.TemporaryDirectory() as directory:
            repo_root = Path(directory)
            (repo_root / changelog_actions.CHANGELOG_PATH).mkdir(parents=True)
            (repo_root / changelog_actions.PARTS_PATH).mkdir(parents=True)
            pull_request = {
                "number": 123,
                "merged": True,
                "merged_at": "2026-08-08T12:00:00Z",
                "body": "Изменение без чейнджлога",
                "html_url": "https://github.com/space-sunrise/sunrise-station/pull/123",
                "user": {"login": "Tester"},
                "base": {"ref": "master"},
            }
            output = io.StringIO()

            with redirect_stdout(output):
                written = changelog_actions.write_pull_request_parts(repo_root, [pull_request], "master")

        self.assertEqual(0, written)
        self.assertIn("::notice::PR #123 пропущен: отсутствует маркер :cl: или 🆑.", output.getvalue())

    def test_pull_request_with_malformed_entry_is_an_error(self):
        with tempfile.TemporaryDirectory() as directory:
            repo_root = Path(directory)
            (repo_root / changelog_actions.CHANGELOG_PATH).mkdir(parents=True)
            (repo_root / changelog_actions.PARTS_PATH).mkdir(parents=True)
            pull_request = {
                "number": 123,
                "merged": True,
                "merged_at": "2026-08-08T12:00:00Z",
                "body": ":cl: Tester\n- add:",
                "html_url": "https://github.com/space-sunrise/sunrise-station/pull/123",
                "user": {"login": "Tester"},
                "base": {"ref": "master"},
            }

            with self.assertRaisesRegex(RuntimeError, "PR #123: не удалось распознать строку чейнжлога"):
                changelog_actions.write_pull_request_parts(repo_root, [pull_request], "master")

    def test_status_is_written_to_actions_log_and_summary(self):
        with tempfile.TemporaryDirectory() as directory:
            summary_path = Path(directory) / "summary.md"
            output = io.StringIO()

            with patch.dict("os.environ", {"GITHUB_STEP_SUMMARY": str(summary_path)}), redirect_stdout(output):
                changelog_actions.report_status("success", "Чейнджлог обновлён.")

            self.assertEqual("::notice::Чейнджлог обновлён.\n", output.getvalue())
            self.assertEqual(
                "## Автоматический чейнджлог\n\n- ✅ Чейнджлог обновлён.\n",
                summary_path.read_text(encoding="utf-8"),
            )

    def test_runtime_error_is_reported_to_actions(self):
        output = io.StringIO()

        with patch.object(changelog_actions, "main", side_effect=RuntimeError("не удалось разобрать PR")), redirect_stdout(
            output,
        ):
            with self.assertRaisesRegex(RuntimeError, "не удалось разобрать PR"):
                changelog_actions.run()

        self.assertIn("::error::Чейнджлог завершился с ошибкой: не удалось разобрать PR", output.getvalue())

    def test_active_sunrise_config_supports_emoji_header_and_keeps_extras_in_main(self):
        parsed = changelog_actions.parse_pr_body(
            "🆑\nADMIN:\n- add: Added",
            "Fallback",
        )

        self.assertEqual(
            (
                "Fallback",
                [
                    changelog_actions.ParsedCategory(
                        "Main",
                        [{"type": "Add", "message": "Added"}],
                    ),
                ],
            ),
            parsed,
        )

    def test_parts_are_processed_by_existing_updater_and_are_idempotent(self):
        with tempfile.TemporaryDirectory() as directory:
            repo_root = Path(directory)
            changelog_dir = repo_root / changelog_actions.CHANGELOG_PATH
            parts_dir = repo_root / changelog_actions.PARTS_PATH
            tools_dir = repo_root / "Tools"
            parts_dir.mkdir(parents=True)
            tools_dir.mkdir()

            for filename in changelog_actions.CATEGORY_FILES.values():
                (changelog_dir / filename).write_text("Entries: []\n", encoding="utf-8")
            (tools_dir / "update_changelog.py").write_bytes(
                (REPO_ROOT / "Tools/update_changelog.py").read_bytes(),
            )

            pull_request = {
                "number": 123,
                "merged": True,
                "merged_at": "2026-07-29T11:16:50Z",
                "body": ":cl: Tester\n- add: Added",
                "html_url": "https://github.com/space-sunrise/sunrise-station/pull/123",
                "user": {"login": "Fallback"},
                "base": {"ref": "master"},
            }

            self.assertEqual(
                1,
                changelog_actions.write_pull_request_parts(repo_root, [pull_request], "master"),
            )
            changelog_actions.update_changelogs(repo_root)

            document = yaml.safe_load(
                (changelog_dir / "ChangelogSunrise.yml").read_text(encoding="utf-8-sig"),
            )
            self.assertEqual("Tester", document["Entries"][0]["author"])
            self.assertEqual("Add", document["Entries"][0]["changes"][0]["type"])
            self.assertEqual("2026-07-29T11:16:50.0000000+00:00", document["Entries"][0]["time"])
            self.assertEqual(
                0,
                changelog_actions.write_pull_request_parts(repo_root, [pull_request], "master"),
            )

    def test_reconciliation_includes_equal_timestamps_and_stops_on_older_updates(self):
        checkpoint = datetime(2026, 7, 29, 11, 16, 50, tzinfo=timezone.utc)
        page = [
            {
                "number": 2,
                "updated_at": "2026-07-30T00:00:00Z",
                "merged_at": "2026-07-29T11:16:50Z",
            },
            {
                "number": 1,
                "updated_at": "2026-07-28T00:00:00Z",
                "merged_at": "2026-07-28T00:00:00Z",
            },
        ]

        with patch.dict("os.environ", {"GITHUB_REPOSITORY": "space-sunrise/sunrise-station"}), patch.object(
            changelog_actions,
            "github_request",
            return_value=page,
        ) as request:
            result = changelog_actions.list_merged_pull_requests(checkpoint)

        self.assertEqual([2], [item["number"] for item in result])
        request.assert_called_once()

    def test_main_resolves_repository_root_after_tool_move(self):
        with tempfile.TemporaryDirectory() as directory:
            repo_root = Path(directory)
            changelog_dir = repo_root / changelog_actions.CHANGELOG_PATH
            parts_dir = repo_root / changelog_actions.PARTS_PATH
            tools_dir = repo_root / "Tools"
            changelog_dir.mkdir(parents=True)
            parts_dir.mkdir(parents=True)
            tools_dir.mkdir()
            (tools_dir / "update_changelog.py").write_bytes(
                (REPO_ROOT / "Tools/update_changelog.py").read_bytes(),
            )
            (changelog_dir / "ChangelogSunrise.yml").write_text("Entries: []\n", encoding="utf-8")
            event_path = repo_root / "event.json"
            event_path.write_text("{}", encoding="utf-8")
            script_path = repo_root / "Tools/_sunrise/changelog/changelog_actions.py"

            with patch.object(changelog_actions, "__file__", str(script_path)), patch.object(
                sys,
                "argv",
                ["changelog_actions.py", "--event-path", str(event_path)],
            ), patch.object(changelog_actions, "list_merged_pull_requests", return_value=[]):
                changelog_actions.main()

            self.assertTrue((repo_root / changelog_actions.STATE_PATH).exists())

    def test_discord_rate_limit_retries_are_bounded(self):
        response = Mock(status_code=429)
        response.json.return_value = {"retry_after": 0}
        response.raise_for_status.side_effect = discord_changelog.requests.HTTPError("rate limited")

        with patch.object(discord_changelog.requests, "post", return_value=response) as post, patch.object(
            discord_changelog.time,
            "sleep",
        ):
            with self.assertRaises(discord_changelog.requests.HTTPError):
                discord_changelog.send_embed_discord({"description": "test"})

        self.assertEqual(discord_changelog.DISCORD_RETRY_LIMIT + 1, post.call_count)
        self.assertTrue(
            all(call.kwargs["timeout"] == discord_changelog.HTTP_REQUEST_TIMEOUT for call in post.call_args_list),
        )

    def test_large_discord_retry_after_fails_without_sleeping_past_deadline(self):
        response = Mock(status_code=429)
        response.json.return_value = {"retry_after": 10**1_000}
        response.raise_for_status.side_effect = discord_changelog.requests.HTTPError("rate limited")

        with patch.object(discord_changelog.requests, "post", return_value=response), patch.object(
            discord_changelog.time,
            "monotonic",
            return_value=100,
        ), patch.object(discord_changelog.time, "sleep") as sleep:
            with self.assertRaises(discord_changelog.requests.HTTPError):
                discord_changelog.send_embed_discord({"description": "test"}, deadline=110)

        sleep.assert_not_called()

    def test_invalid_discord_retry_after_uses_safe_default(self):
        invalid_values = ("invalid", float("nan"), -1, True, None)

        for retry_after in invalid_values:
            with self.subTest(retry_after=retry_after):
                rate_limited = Mock(status_code=429)
                rate_limited.json.return_value = {"retry_after": retry_after}
                sent = Mock(status_code=204)

                with patch.object(
                    discord_changelog.requests,
                    "post",
                    side_effect=(rate_limited, sent),
                ), patch.object(discord_changelog.time, "sleep") as sleep:
                    discord_changelog.send_embed_discord({"description": "test"})

                sleep.assert_called_once_with(discord_changelog.DISCORD_DEFAULT_RETRY_AFTER)

    def test_unexpected_discord_status_keeps_status_code(self):
        response = Mock(status_code=200)

        with patch.object(discord_changelog.requests, "post", return_value=response):
            with self.assertRaises(discord_changelog.UnexpectedDiscordStatusError) as raised:
                discord_changelog.send_embed_discord({"description": "test"})

        self.assertEqual(200, raised.exception.status_code)
        self.assertEqual("Discord webhook вернул неожиданный статус 200", str(raised.exception))

    def test_manual_and_reader_timestamps_support_optional_microseconds(self):
        self.assertRegex(manual_changelog.make_timestamp(), r"\.\d{6}\+00:00$")

        with tempfile.TemporaryDirectory() as directory:
            repo_root = Path(directory)
            changelog_path = repo_root / "Resources/Changelog/ChangelogSunrise.yml"
            changelog_path.parent.mkdir(parents=True)
            changelog_path.write_text(
                """Entries:
- author: Tester
  time: "2026-08-08T12:00:00+00:00"
  changes: []
- author: Tester
  time: "2026-08-08T12:00:00.123456+00:00"
  changes: []
""",
                encoding="utf-8",
            )
            result = subprocess.run(
                [sys.executable, str(REPO_ROOT / "Tools/_sunrise/changelog/read_changelog.py")],
                cwd=repo_root,
                check=True,
                capture_output=True,
                text=True,
            )

        self.assertNotIn("Error formatting time", result.stdout)

    def test_workflow_has_all_entry_points_and_safe_app_write_retry(self):
        workflow = WORKFLOW_PATH.read_text(encoding="utf-8")
        runner = RUNNER_PATH.read_text(encoding="utf-8")
        document = yaml.load(workflow, Loader=yaml.BaseLoader)

        self.assertIn("on", document)
        self.assertIn("jobs", document)
        self.assertIn("pull_request_target:", workflow)
        self.assertIn("push:", workflow)
        self.assertIn("workflow_dispatch:", workflow)
        self.assertIn("schedule:", workflow)
        self.assertIn("permissions: {}", workflow)
        self.assertIn(
            "actions/create-github-app-token@bcd2ba49218906704ab6c1aa796996da409d3eb1 # v3.2.0",
            workflow,
        )
        self.assertIn("actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683 # v4.2.2", workflow)
        self.assertIn(
            "actions/setup-python@a26af69be951a213d495a4c3e4e4022e16d87065 # v5.6.0",
            workflow,
        )
        self.assertNotRegex(
            workflow,
            r"uses: actions/(?:create-github-app-token|checkout|setup-python)@v",
        )
        self.assertIn("client-id: ${{ vars.CHANGELOG_APP_CLIENT_ID }}", workflow)
        self.assertIn("private-key: ${{ secrets.CHANGELOG_APP_PRIVATE_KEY }}", workflow)
        self.assertIn("permission-contents: write", workflow)
        self.assertIn("permission-pull-requests: read", workflow)
        self.assertIn("token: ${{ steps.app-token.outputs.token }}", workflow)
        self.assertNotIn("CHANGELOG_TOKEN", workflow)
        self.assertNotIn("CHANGELOG_SSH_KEY", workflow)
        self.assertNotIn("github.token", workflow)
        self.assertNotIn("concurrency:", workflow)
        self.assertIn("run: bash Tools/_sunrise/changelog/run.sh", workflow)
        self.assertNotIn("git reset --hard origin/master", workflow)
        self.assertIn("git reset --hard origin/master", runner)
        self.assertIn("for attempt in {1..5}", runner)
        self.assertIn("python Tools/_sunrise/changelog/changelog_actions.py", runner)
        self.assertIn("Чейнджлог уже актуален: публикация не требуется.", runner)
        self.assertIn("Чейнджлог успешно опубликован в master.", runner)
        self.assertIn("Не удалось отправить чейнджлог после пяти попыток.", runner)
        self.assertNotIn("github.event.pull_request.head", workflow)


if __name__ == "__main__":
    unittest.main()
