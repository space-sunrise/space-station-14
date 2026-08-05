import tempfile
import unittest
from datetime import datetime, timezone
from pathlib import Path
from unittest.mock import patch

import yaml

import changelog_actions


REPO_ROOT = Path(__file__).resolve().parents[2]
WORKFLOW_PATH = REPO_ROOT / ".github/workflows/changelog.yml"


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

    def test_workflow_has_all_entry_points_and_safe_app_write_retry(self):
        workflow = WORKFLOW_PATH.read_text(encoding="utf-8")
        document = yaml.load(workflow, Loader=yaml.BaseLoader)

        self.assertIn("on", document)
        self.assertIn("jobs", document)
        self.assertIn("pull_request_target:", workflow)
        self.assertIn("push:", workflow)
        self.assertIn("workflow_dispatch:", workflow)
        self.assertIn("schedule:", workflow)
        self.assertIn("permissions: {}", workflow)
        self.assertIn("actions/create-github-app-token@v3.2.0", workflow)
        self.assertIn("client-id: ${{ vars.CHANGELOG_APP_CLIENT_ID }}", workflow)
        self.assertIn("private-key: ${{ secrets.CHANGELOG_APP_PRIVATE_KEY }}", workflow)
        self.assertIn("token: ${{ steps.app-token.outputs.token }}", workflow)
        self.assertNotIn("CHANGELOG_TOKEN", workflow)
        self.assertNotIn("CHANGELOG_SSH_KEY", workflow)
        self.assertNotIn("github.token", workflow)
        self.assertNotIn("concurrency:", workflow)
        self.assertIn("git reset --hard origin/master", workflow)
        self.assertIn("for attempt in {1..5}", workflow)
        self.assertNotIn("github.event.pull_request.head", workflow)


if __name__ == "__main__":
    unittest.main()
