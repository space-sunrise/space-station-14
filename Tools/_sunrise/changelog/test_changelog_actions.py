import io
import json
import os
import subprocess
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from datetime import datetime, timezone
from pathlib import Path
from unittest.mock import Mock, call, patch

import yaml

os.environ["CHANGELOG_FILE"] = "Resources/Changelog/ChangelogSunrise.yml"

import actions_changelogs_since_last_run as discord_changelog
import changelog_actions
import manual_changelog


REPO_ROOT = Path(__file__).resolve().parents[3]
WORKFLOW_PATH = REPO_ROOT / ".github/workflows/changelog.yml"
PUBLISH_WORKFLOW_PATH = REPO_ROOT / ".github/workflows/publish-stable.yml"
DISCORD_WORKFLOW_PATH = REPO_ROOT / ".github/workflows/publish-discord-changelog.yml"
RUNNER_PATH = REPO_ROOT / "Tools/_sunrise/changelog/run.sh"


class ChangelogActionsTests(unittest.TestCase):
    def test_changelog_file_must_stay_inside_repository(self):
        for invalid_path in (
            "../outside.yml",
            "/outside.yml",
            "C:/outside.yml",
            "C:outside.yml",
            "\\outside.yml",
        ):
            with self.subTest(invalid_path=invalid_path), patch.dict(
                "os.environ", {"CHANGELOG_FILE": invalid_path}
            ):
                with self.assertRaisesRegex(RuntimeError, "относительным путём внутри репозитория"):
                    changelog_actions.configured_changelog_file()

        with patch.dict("os.environ", {"CHANGELOG_FILE": "Changelog.yml"}):
            with self.assertRaisesRegex(RuntimeError, "родительский каталог"):
                changelog_actions.configured_changelog_file()

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

    def test_parser_accepts_russian_author_and_text(self):
        parsed = changelog_actions.parse_pr_body(
            ":cl: Иван Иванов\n- add: Добавлена очень важная рыба",
            "Fallback",
        )

        self.assertEqual(
            (
                "Иван Иванов",
                [
                    changelog_actions.ParsedCategory(
                        "Main",
                        [{"type": "Add", "message": "Добавлена очень важная рыба"}],
                    ),
                ],
            ),
            parsed,
        )

    def test_parser_preserves_multiline_change_text(self):
        parsed = changelog_actions.parse_pr_body(
            ":cl: Иван\n"
            "- add: Первая строка\n"
            "Вторая строка\n"
            "  Третья строка\n"
            "- fix: Другая запись",
            "Fallback",
        )

        self.assertEqual(
            "Первая строка\nВторая строка\nТретья строка",
            parsed[1][0].changes[0]["message"],
        )
        self.assertEqual("Другая запись", parsed[1][0].changes[1]["message"])

    def test_comments_with_greater_than_are_removed(self):
        body = ":cl: Иван\n- add: Начало <!-- 2 > 1 --> конец"

        parsed = changelog_actions.parse_pr_body(body, "Fallback")

        self.assertEqual("Начало  конец", parsed[1][0].changes[0]["message"])
        self.assertTrue(
            changelog_actions.is_changelog_template(
                body,
                ":cl: Иван\n- add: Начало  конец",
            ),
        )
        self.assertIsNone(changelog_actions.COMMENT_RE.search(r"\<!-- не комментарий -->"))
        self.assertEqual(
            r"<!-- первый \--> настоящий конец -->",
            changelog_actions.COMMENT_RE.search(
                r"<!-- первый \--> настоящий конец -->",
            ).group(),
        )

    def test_parser_ignores_changelog_inside_fenced_code(self):
        for fence in ("```md", "~~~text"):
            with self.subTest(fence=fence):
                body = (
                    f"{fence}\r\n"
                    ":cl: Пример\r\n"
                    "- add: Не настоящий чейнджлог\r\n"
                    f"{fence[:3]}\r\n"
                    "\r\n"
                    ":cl: Настоящий автор\r\n"
                    "- fix: Настоящее исправление\r\n"
                    ":end-cl:\r\n"
                )

                parsed = changelog_actions.parse_pr_body(body, "Fallback")

                self.assertEqual("Настоящий автор", parsed[0])
                self.assertEqual(
                    [{"type": "Fix", "message": "Настоящее исправление"}],
                    parsed[1][0].changes,
                )

        self.assertIsNone(
            changelog_actions.parse_pr_body(
                "```md\n:cl: Только пример\n- add: Не публиковать\n```",
                "Fallback",
            ),
        )

    def test_template_check_ignores_fenced_example_and_text_after_boundary(self):
        template = ":cl: ВАШЕ_ИМЯ\n- add: ТЕКСТ\n:end-cl:"
        body = (
            "```md\n:cl: Пример\n- add: Не настоящий чейнджлог\n```\n"
            ":cl: ВАШЕ_ИМЯ\n"
            "<!-- Комментарии при сравнении не учитываются. -->\n"
            "- add: ТЕКСТ\n"
            ":end-cl:\n"
            "## Summary by CodeRabbit\n"
            "Этот текст находится за границей."
        )

        self.assertTrue(changelog_actions.is_changelog_template(body, template))

    def test_parser_stops_at_explicit_or_double_blank_boundary(self):
        for body in (
            ":cl: Иван\n- add: Нужная запись\n:end-cl:\n- fix: Не чейнжлог",
            ":cl: Иван\n- add: Нужная запись\n\n\n- fix: Не чейнжлог",
        ):
            with self.subTest(body=body):
                parsed = changelog_actions.parse_pr_body(body, "Fallback")
                self.assertEqual(
                    [{"type": "Add", "message": "Нужная запись"}],
                    parsed[1][0].changes,
                )

    def test_parser_keeps_legacy_body_without_boundary_and_ignores_comment_spacing(self):
        parsed = changelog_actions.parse_pr_body(
            ":cl: Иван\n"
            "<!-- Комментарий можно оставить. -->\n"
            "- add: Первая запись\n"
            "<!-- Комментарий не считается пустой строкой. -->\n"
            "\n"
            "- fix: Вторая запись",
            "Fallback",
        )

        self.assertEqual(
            [
                {"type": "Add", "message": "Первая запись"},
                {"type": "Fix", "message": "Вторая запись"},
            ],
            parsed[1][0].changes,
        )

    def test_parser_attaches_media_to_preceding_change(self):
        parsed = changelog_actions.parse_pr_body(
            ":cl: Иван\n"
            "- add: Добавлена новая рыба\n"
            "media: https://example.org/fish.mp4\n"
            "media: ![Рыба в игре](https://example.org/fish.png)\n"
            "media: [Демонстрация](https://example.org/demo.webm)\n"
            "ADMIN:\n"
            "- fix: Исправлен доступ\n"
            "media: ![Админская панель](https://example.org/admin.gif)",
            "Fallback",
            ("Main", "Admin"),
        )

        self.assertEqual("Иван", parsed[0])
        self.assertEqual([
            {"url": "https://example.org/fish.mp4", "change": 0},
            {"url": "https://example.org/fish.png", "description": "Рыба в игре", "change": 0},
            {"url": "https://example.org/demo.webm", "description": "Демонстрация", "change": 0},
        ], parsed[1][0].media)
        self.assertEqual(
            [{"url": "https://example.org/admin.gif", "description": "Админская панель", "change": 0}],
            parsed[1][1].media,
        )

    def test_parser_rejects_media_without_changes(self):
        with self.assertRaisesRegex(ValueError, "категория Admin содержит медиа"):
            changelog_actions.parse_pr_body(
                ":cl: Иван\nADMIN:\nmedia: https://example.org/admin.png",
                "Fallback",
                ("Main", "Admin"),
            )

    def test_manual_parser_requires_ci_author_and_supports_media(self):
        author, categories = changelog_actions.parse_manual_changelog(
            ":ci: Иван Иванов\n"
            "- add: Добавлена рыба\n"
            "media: ![Рыба](https://example.org/fish.png)\n"
            "ADMIN:\n"
            "- fix: Исправлена команда\n"
            "media: https://example.org/demo.webm",
            ("Main", "Admin"),
        )

        self.assertEqual("Иван Иванов", author)
        self.assertEqual(["Main", "Admin"], [category.name for category in categories])
        self.assertEqual("Рыба", categories[0].media[0]["description"])
        self.assertEqual("https://example.org/demo.webm", categories[1].media[0]["url"])

    def test_manual_parser_cleans_author_comment_and_preserves_multiline_text(self):
        author, categories = changelog_actions.parse_manual_changelog(
            ":ci: Иван <!-- служебный комментарий: 2 > 1 -->\n"
            "- add: Первая строка\n"
            "Вторая строка",
        )

        self.assertEqual("Иван", author)
        self.assertEqual("Первая строка\nВторая строка", categories[0].changes[0]["message"])

        for body, error in (
            (":cl: Иван\n- add: Рыба", "должен начинаться со строки :ci: Автор"),
            (":ci:\n- add: Рыба", "необходимо указать имя автора"),
        ):
            with self.subTest(body=body), self.assertRaisesRegex(ValueError, error):
                changelog_actions.parse_manual_changelog(body)

    def test_manual_changelog_is_written_once_per_actions_run(self):
        with tempfile.TemporaryDirectory() as directory:
            repo_root = Path(directory)
            changelog_dir = repo_root / changelog_actions.CHANGELOG_PATH
            parts_dir = repo_root / changelog_actions.PARTS_PATH
            parts_dir.mkdir(parents=True)
            for filename in ("ChangelogSunrise.yml", "Admin.yml"):
                (changelog_dir / filename).write_text("Entries: []\n", encoding="utf-8")

            categories = {"Main": "ChangelogSunrise.yml", "Admin": "Admin.yml"}
            body = ":ci: Иван\n- add: Рыба\nADMIN:\n- fix: Команда"
            environment = {
                "GITHUB_REPOSITORY": "space-sunrise/sunrise-station",
                "GITHUB_RUN_ID": "123456",
            }
            with patch.dict("os.environ", environment):
                self.assertEqual(2, changelog_actions.write_manual_parts(repo_root, body, categories))

            main_part = yaml.safe_load((parts_dir / "manual-123456-Main.yml").read_text(encoding="utf-8"))
            admin_part = yaml.safe_load((parts_dir / "manual-123456-Admin.yml").read_text(encoding="utf-8"))
            self.assertEqual("Иван", main_part["author"])
            self.assertNotIn("category", main_part)
            self.assertEqual("Admin", admin_part["category"])
            self.assertEqual(
                "https://github.com/space-sunrise/sunrise-station/actions/runs/123456",
                main_part["url"],
            )

            (changelog_dir / "ChangelogSunrise.yml").write_text(
                yaml.safe_dump({"Entries": [{"url": main_part["url"]}]}),
                encoding="utf-8",
            )
            (changelog_dir / "Admin.yml").write_text(
                yaml.safe_dump({"Entries": [{"url": admin_part["url"]}]}),
                encoding="utf-8",
            )
            for path in parts_dir.glob("*.yml"):
                path.unlink()

            with patch.dict("os.environ", environment):
                self.assertEqual(0, changelog_actions.write_manual_parts(repo_root, body, categories))
            self.assertEqual([], list(parts_dir.glob("*.yml")))

    def test_unchanged_changelog_template_is_logged_as_skip(self):
        with tempfile.TemporaryDirectory() as directory:
            repo_root = Path(directory)
            template_path = repo_root / changelog_actions.PULL_REQUEST_TEMPLATE_PATH
            template_path.parent.mkdir(parents=True)
            template_path.write_text(
                "Описание\n\n:cl: ВАШЕ_ИМЯ\n- add: ТЕКСТ\n- fix: ТЕКСТ\n",
                encoding="utf-8",
            )
            pull_request = {
                "number": 123,
                "merged": True,
                "merged_at": "2026-08-08T12:00:00Z",
                "body": (
                    "Заполненное описание\n\n"
                    ":cl:\u200b ВАШЕ_ИМЯ\n"
                    "\t-\tadd:\u00a0ТЕКСТ\n"
                    " - fix: ТЕКСТ\ufeff\n"
                ),
                "html_url": "https://github.com/space-sunrise/sunrise-station/pull/123",
                "user": {"login": "Tester"},
                "base": {"ref": "master"},
            }
            output = io.StringIO()

            with redirect_stdout(output):
                written = changelog_actions.write_pull_request_parts(repo_root, [pull_request], "master")

        self.assertEqual(0, written)
        self.assertIn("::notice::PR #123 пропущен: оставлен шаблон чейнжлога.", output.getvalue())

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
            updater_path = repo_root / changelog_actions.UPDATER_PATH
            parts_dir.mkdir(parents=True)
            updater_path.parent.mkdir(parents=True)

            for filename in changelog_actions.CATEGORY_FILES.values():
                (changelog_dir / filename).write_text("Entries: []\n", encoding="utf-8")
            updater_path.write_bytes(
                (REPO_ROOT / changelog_actions.UPDATER_PATH).read_bytes(),
            )

            pull_request = {
                "number": 123,
                "merged": True,
                "merged_at": "2026-07-29T11:16:50Z",
                "body": (
                    ":cl: Tester\n"
                    "- add: Added\n"
                    "media: ![Рыба](https://example.org/fish.png)"
                ),
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
            self.assertEqual(
                [{"url": "https://example.org/fish.png", "description": "Рыба", "change": 0}],
                document["Entries"][0]["media"],
            )
            self.assertEqual("2026-07-29T11:16:50.0000000+00:00", document["Entries"][0]["time"])
            self.assertEqual(
                0,
                changelog_actions.write_pull_request_parts(repo_root, [pull_request], "master"),
            )

    def test_updater_handles_null_entries_deterministically_and_keeps_unicode(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            changelog = root / "Changelog.yml"
            parts = root / "Parts"
            parts.mkdir()
            changelog.write_text("Entries:\nOther: Значение\n", encoding="utf-8")
            for filename, author in (("z.yml", "Яна"), ("a.yml", "Алиса")):
                (parts / filename).write_text(
                    yaml.safe_dump(
                        {"author": author, "changes": [{"type": "Add", "message": "Рыба"}]},
                        allow_unicode=True,
                        sort_keys=False,
                    ),
                    encoding="utf-8",
                )

            subprocess.run(
                [
                    sys.executable,
                    str(REPO_ROOT / "Tools/_sunrise/changelog/update_changelog.py"),
                    str(changelog),
                    str(parts),
                ],
                check=True,
                capture_output=True,
                text=True,
            )

            text = changelog.read_text(encoding="utf-8-sig")
            document = yaml.safe_load(text)

        self.assertEqual(["Алиса", "Яна"], [entry["author"] for entry in document["Entries"]])
        self.assertEqual([1, 2], [entry["id"] for entry in document["Entries"]])
        self.assertLess(text.index("Entries:"), text.index("Other:"))
        self.assertIn("Алиса", text)

    def test_updater_ignores_existing_entry_without_id(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            changelog = root / "Changelog.yml"
            parts = root / "Parts"
            parts.mkdir()
            changelog.write_text(
                "Entries:\n- author: Без идентификатора\n- author: С идентификатором\n  id: 7\n",
                encoding="utf-8",
            )
            (parts / "part.yml").write_text(
                "author: Новая запись\nchanges:\n- type: Add\n  message: Добавлено\n",
                encoding="utf-8",
            )

            subprocess.run(
                [
                    sys.executable,
                    str(REPO_ROOT / "Tools/_sunrise/changelog/update_changelog.py"),
                    str(changelog),
                    str(parts),
                ],
                check=True,
                capture_output=True,
                text=True,
            )
            document = yaml.safe_load(changelog.read_text(encoding="utf-8-sig"))

        self.assertEqual(8, document["Entries"][-1]["id"])

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

    def test_checkpoint_uses_previous_successful_actions_run(self):
        response = {
            "workflow_runs": [
                {
                    "id": 200,
                    "event": "pull_request_target",
                    "created_at": "2026-08-11T13:00:00Z",
                    "run_started_at": "2026-08-11T13:01:00Z",
                },
                {
                    "id": 150,
                    "event": "workflow_dispatch",
                    "created_at": "2026-08-11T12:30:00Z",
                    "run_started_at": "2026-08-11T12:31:00Z",
                },
                {
                    "id": 125,
                    "event": "pull_request_target",
                    "created_at": "2026-08-11T12:15:00Z",
                    "run_started_at": "2026-08-11T12:16:00Z",
                },
                {
                    "id": 100,
                    "event": "pull_request_target",
                    "created_at": "2026-08-11T12:00:00Z",
                    "run_started_at": "2026-08-11T12:01:00Z",
                },
            ],
        }

        def request_response(path, *_args, **_kwargs):
            if path.endswith("/actions/workflows/changelog.yml/runs"):
                return response
            if path.endswith("/actions/runs/125/jobs"):
                return {
                    "jobs": [
                        {"name": "update", "status": "completed", "conclusion": "skipped"},
                    ],
                }
            if path.endswith("/actions/runs/100/jobs"):
                return {
                    "jobs": [
                        {"name": "update", "status": "completed", "conclusion": "success"},
                    ],
                }
            self.fail(f"Неожиданный запрос: {path}")

        with patch.dict(
            "os.environ",
            {
                "GITHUB_REPOSITORY": "space-sunrise/sunrise-station",
                "GITHUB_RUN_ID": "200",
            },
        ), patch.object(changelog_actions, "github_request", side_effect=request_response) as request:
            checkpoint = changelog_actions.load_checkpoint(Path("."))

        self.assertEqual(datetime(2026, 8, 11, 12, 1, tzinfo=timezone.utc), checkpoint)
        self.assertEqual(
            [
                call(
                    "/repos/space-sunrise/sunrise-station/actions/workflows/changelog.yml/runs",
                    {"status": "success", "per_page": 100},
                    token_environment="ACTIONS_TOKEN",
                ),
                call(
                    "/repos/space-sunrise/sunrise-station/actions/runs/125/jobs",
                    {"per_page": 100},
                    token_environment="ACTIONS_TOKEN",
                ),
                call(
                    "/repos/space-sunrise/sunrise-station/actions/runs/100/jobs",
                    {"per_page": 100},
                    token_environment="ACTIONS_TOKEN",
                ),
            ],
            request.call_args_list,
        )

    def test_discord_api_rejects_unsafe_changelog_path(self):
        session = Mock()

        with patch.object(discord_changelog, "CHANGELOG_FILE", "../outside.yml"):
            with self.assertRaisesRegex(RuntimeError, "относительным путём внутри репозитория"):
                discord_changelog.get_last_changelog_by_sha(session, "abc", "space-sunrise/repo")

        session.get.assert_not_called()

    def test_discord_api_encodes_changelog_path(self):
        session = Mock()

        discord_changelog.get_last_changelog_by_sha(
            session,
            "abc",
            "space-sunrise/repo",
            Path("Resources/Changelog/Changelog?test.yml"),
        )

        session.get.assert_called_once_with(
            "https://api.github.com/repos/space-sunrise/repo/contents/"
            "Resources/Changelog/Changelog%3Ftest.yml",
            headers={"Accept": "application/vnd.github.raw"},
            params={"ref": "abc"},
            timeout=discord_changelog.HTTP_REQUEST_TIMEOUT,
        )

    def test_first_discord_workflow_uses_previous_stable_publish(self):
        previous = {"id": 100, "head_commit": {"id": "abc"}}

        with patch.dict(
            "os.environ",
            {
                "GITHUB_REPOSITORY": "space-sunrise/sunrise-station",
                "GITHUB_RUN_ID": "300",
                "GITHUB_TOKEN": "token",
                "SOURCE_WORKFLOW_RUN_ID": "200",
            },
        ), patch.object(
            discord_changelog,
            "get_most_recent_workflow",
            side_effect=[None, previous],
        ) as recent, patch.object(
            discord_changelog,
            "get_last_changelog_by_sha",
            return_value="Entries: []",
        ) as load:
            result = discord_changelog.get_last_changelog(Path("Resources/Changelog/ChangelogSunrise.yml"))

        self.assertEqual("Entries: []", result)
        self.assertEqual("300", recent.call_args_list[0].args[2])
        self.assertEqual("200", recent.call_args_list[1].args[2])
        self.assertEqual("abc", load.call_args.args[1])

    def test_discord_workflow_keeps_last_successful_released_sha(self):
        sha = "a" * 40
        previous = {
            "id": 250,
            "display_title": f"Discord changelog for {sha}",
            "head_commit": {"id": "default-branch-sha"},
        }

        with patch.dict(
            "os.environ",
            {
                "GITHUB_REPOSITORY": "space-sunrise/sunrise-station",
                "GITHUB_RUN_ID": "300",
                "GITHUB_TOKEN": "token",
                "SOURCE_WORKFLOW_RUN_ID": "200",
            },
        ), patch.object(
            discord_changelog,
            "get_most_recent_workflow",
            return_value=previous,
        ) as recent, patch.object(
            discord_changelog,
            "get_last_changelog_by_sha",
            return_value="Entries: []",
        ) as load:
            discord_changelog.get_last_changelog(Path("Resources/Changelog/ChangelogSunrise.yml"))

        recent.assert_called_once()
        self.assertEqual(sha, load.call_args.args[1])

    def test_discord_workflow_falls_back_when_previous_title_has_no_sha(self):
        previous_discord = {"id": 250, "display_title": "Старое имя запуска"}
        previous_stable = {"id": 100, "head_commit": {"id": "stable-sha"}}

        with patch.dict(
            "os.environ",
            {
                "GITHUB_REPOSITORY": "space-sunrise/sunrise-station",
                "GITHUB_RUN_ID": "300",
                "GITHUB_TOKEN": "token",
                "SOURCE_WORKFLOW_RUN_ID": "200",
            },
        ), patch.object(
            discord_changelog,
            "get_most_recent_workflow",
            side_effect=[previous_discord, previous_stable],
        ) as recent, patch.object(
            discord_changelog,
            "get_last_changelog_by_sha",
            return_value="Entries: []",
        ) as load:
            discord_changelog.get_last_changelog(Path("Resources/Changelog/ChangelogSunrise.yml"))

        self.assertEqual(["300", "200"], [item.args[2] for item in recent.call_args_list])
        self.assertEqual("stable-sha", load.call_args.args[1])

    def test_checkpoint_falls_back_to_latest_changelog_entry(self):
        with tempfile.TemporaryDirectory() as directory:
            repo_root = Path(directory)
            changelog_dir = repo_root / changelog_actions.CHANGELOG_PATH
            changelog_dir.mkdir(parents=True)
            (changelog_dir / "ChangelogSunrise.yml").write_text(
                'Entries:\n- time: "2026-08-10T12:00:00Z"\n',
                encoding="utf-8",
            )

            with patch.dict(
                "os.environ",
                {"GITHUB_REPOSITORY": "space-sunrise/sunrise-station"},
            ), patch.object(
                changelog_actions,
                "github_request",
                return_value={"workflow_runs": []},
            ):
                checkpoint = changelog_actions.load_checkpoint(repo_root)

        self.assertEqual(datetime(2026, 8, 10, 12, tzinfo=timezone.utc), checkpoint)

    def test_main_resolves_repository_root_after_tool_move(self):
        with tempfile.TemporaryDirectory() as directory:
            repo_root = Path(directory)
            changelog_dir = repo_root / changelog_actions.CHANGELOG_PATH
            parts_dir = repo_root / changelog_actions.PARTS_PATH
            updater_path = repo_root / changelog_actions.UPDATER_PATH
            changelog_dir.mkdir(parents=True)
            parts_dir.mkdir(parents=True)
            updater_path.parent.mkdir(parents=True)
            updater_path.write_bytes(
                (REPO_ROOT / changelog_actions.UPDATER_PATH).read_bytes(),
            )
            (changelog_dir / "ChangelogSunrise.yml").write_text("Entries: []\n", encoding="utf-8")
            event_path = repo_root / "event.json"
            event_path.write_text("{}", encoding="utf-8")
            script_path = repo_root / "Tools/_sunrise/changelog/changelog_actions.py"

            with patch.object(changelog_actions, "__file__", str(script_path)), patch.object(
                sys,
                "argv",
                ["changelog_actions.py", "--event-path", str(event_path)],
            ), patch.object(
                changelog_actions,
                "load_checkpoint",
                return_value=datetime(2026, 8, 11, tzinfo=timezone.utc),
            ), patch.object(changelog_actions, "list_merged_pull_requests", return_value=[]):
                changelog_actions.main()

            self.assertEqual(
                {"Entries": []},
                yaml.safe_load((changelog_dir / "ChangelogSunrise.yml").read_text(encoding="utf-8")),
            )

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

    def test_discord_accepts_message_and_no_content_statuses(self):
        for status_code in (200, 204):
            with self.subTest(status_code=status_code):
                response = Mock(status_code=status_code)
                with patch.object(discord_changelog.requests, "post", return_value=response):
                    discord_changelog.send_embed_discord({"description": "test"})

    def test_unexpected_discord_status_keeps_status_code(self):
        response = Mock(status_code=201)

        with patch.object(discord_changelog.requests, "post", return_value=response):
            with self.assertRaises(discord_changelog.UnexpectedDiscordStatusError) as raised:
                discord_changelog.send_embed_discord({"description": "test"})

        self.assertEqual(201, raised.exception.status_code)
        self.assertEqual("Discord webhook вернул неожиданный статус 201", str(raised.exception))

    def test_media_url_rejections_do_not_resolve_or_connect(self):
        invalid_urls = (
            "http://example.org/file.png",
            "https://user:password@example.org/file.png",
            "https://example.org:8443/file.png",
            "https://127.0.0.1/file.png",
        )

        with patch.object(discord_changelog.socket, "getaddrinfo") as resolve, patch.object(
            discord_changelog, "_VerifiedHTTPSConnection"
        ) as connection:
            for url in invalid_urls:
                with self.subTest(url=url), self.assertRaises(discord_changelog.MediaError):
                    discord_changelog.download_media(url)

        resolve.assert_not_called()
        connection.assert_not_called()

    def test_media_dns_rebinding_on_redirect_is_rejected(self):
        redirect = Mock(status=302)
        redirect.getheader.return_value = "https://next.example/file.png"
        connection = Mock()
        connection.getresponse.return_value = redirect

        with patch.object(
            discord_changelog,
            "_resolve_public_address",
            side_effect=[(discord_changelog.socket.AF_INET, "93.184.216.34"), discord_changelog.MediaError("private")],
        ), patch.object(discord_changelog, "_VerifiedHTTPSConnection", return_value=connection) as connect:
            with self.assertRaises(discord_changelog.MediaError):
                discord_changelog.download_media("https://example.org/file.png")

        connect.assert_called_once()
        connection.request.assert_called_once()

    def test_media_redirect_limit_is_checked_before_extra_request(self):
        responses = []
        for index in range(discord_changelog.MEDIA_MAX_REDIRECTS + 1):
            response = Mock(status=302)
            response.getheader.return_value = f"https://redirect-{index}.example/file.png"
            responses.append(response)
        connection = Mock()
        connection.getresponse.side_effect = responses

        with patch.object(
            discord_changelog,
            "_resolve_public_address",
            return_value=(discord_changelog.socket.AF_INET, "93.184.216.34"),
        ), patch.object(discord_changelog, "_VerifiedHTTPSConnection", return_value=connection):
            with self.assertRaisesRegex(discord_changelog.MediaError, "превышено число перенаправлений"):
                discord_changelog.download_media("https://example.org/file.png")

        self.assertEqual(discord_changelog.MEDIA_MAX_REDIRECTS + 1, connection.request.call_count)

    def test_media_size_and_signature_are_checked_after_streaming(self):
        too_large = Mock(status=200)
        too_large.getheader.side_effect = lambda name: str(discord_changelog.MEDIA_MAX_SIZE + 1) if name == "Content-Length" else None
        too_large_connection = Mock()
        too_large_connection.getresponse.return_value = too_large

        with patch.object(
            discord_changelog,
            "_resolve_public_address",
            return_value=(discord_changelog.socket.AF_INET, "93.184.216.34"),
        ), patch.object(discord_changelog, "_VerifiedHTTPSConnection", return_value=too_large_connection):
            with self.assertRaisesRegex(discord_changelog.MediaError, "превышает лимит"):
                discord_changelog.download_media("https://example.org/file.bin")
        too_large.read.assert_not_called()

        unknown = Mock(status=200)
        unknown.getheader.return_value = None
        unknown.read.side_effect = [b"not a supported file", b""]
        unknown_connection = Mock()
        unknown_connection.getresponse.return_value = unknown
        with patch.object(
            discord_changelog,
            "_resolve_public_address",
            return_value=(discord_changelog.socket.AF_INET, "93.184.216.34"),
        ), patch.object(discord_changelog, "_VerifiedHTTPSConnection", return_value=unknown_connection):
            with self.assertRaisesRegex(discord_changelog.MediaError, "сигнатура файла"):
                discord_changelog.download_media("https://example.org/file.bin")

    def test_media_payload_uses_attachments_for_images_and_videos(self):
        image = discord_changelog.DownloadedMedia(
            "https://example.org/image.png", "Изображение", b"png", "image/png", "media-1.png"
        )
        video = discord_changelog.DownloadedMedia(
            "https://example.org/video.mp4", None, b"mp4", "video/mp4", "media-2.mp4"
        )
        text_embed = {"title": "Автор", "description": "Текст"}

        payload, files = discord_changelog.build_media_payload(text_embed, [image, video])

        container = payload["components"][0]
        self.assertEqual(discord_changelog.DISCORD_COMPONENTS_V2_FLAG, payload["flags"])
        self.assertEqual("### Автор", container["components"][0]["content"])
        self.assertEqual("Текст", container["components"][1]["content"])
        self.assertEqual(
            "attachment://media-1.png",
            container["components"][2]["items"][0]["media"]["url"],
        )
        self.assertEqual(
            "attachment://media-2.mp4",
            container["components"][2]["items"][1]["media"]["url"],
        )
        self.assertEqual(
            [
                {"id": 0, "filename": "media-1.png", "description": "Изображение"},
                {"id": 1, "filename": "media-2.mp4"},
            ],
            payload["attachments"],
        )
        self.assertEqual(["files[0]", "files[1]"], [field for field, _ in files])
        self.assertEqual(("media-2.mp4", b"mp4", "video/mp4"), files[1][1])

    def test_media_gallery_keeps_author_order_regardless_of_type(self):
        video = discord_changelog.DownloadedMedia(
            "video", None, b"webm", "video/webm", "media-1.webm"
        )
        image = discord_changelog.DownloadedMedia(
            "image", None, b"png", "image/png", "media-2.png"
        )

        payload, _ = discord_changelog.build_media_payload(None, [video, image])

        items = payload["components"][0]["components"][0]["items"]
        self.assertEqual(
            ["attachment://media-1.webm", "attachment://media-2.png"],
            [item["media"]["url"] for item in items],
        )

    def test_remote_service_link_stays_in_authored_order(self):
        youtube = discord_changelog.RemoteMedia(
            "https://www.youtube.com/watch?v=example", "YouTube", change_index=0
        )
        image = discord_changelog.DownloadedMedia(
            "image", None, b"png", "image/png", "media-2.png", change_index=0
        )
        vimeo = discord_changelog.RemoteMedia(
            "https://vimeo.com/example", "Vimeo", change_index=0
        )

        payload, files = discord_changelog.build_media_payload(None, [youtube, image, vimeo])

        components = payload["components"][0]["components"]
        self.assertEqual(
            "🔗 **YouTube**\n<https://www.youtube.com/watch?v=example>",
            components[0]["content"],
        )
        self.assertEqual("attachment://media-2.png", components[1]["items"][0]["media"]["url"])
        self.assertEqual(
            "🔗 **Vimeo**\n<https://vimeo.com/example>",
            components[2]["content"],
        )
        self.assertEqual([{"id": 0, "filename": "media-2.png"}], payload["attachments"])
        self.assertEqual(["files[0]"], [field for field, _ in files])

    def test_unsupported_download_becomes_remote_service_link(self):
        entry = {
            "changes": [{"type": "Add", "message": "Видео"}],
            "media": [{"url": "https://www.youtube.com/watch?v=example", "change": 0}],
        }

        with patch.object(
            discord_changelog,
            "download_media",
            side_effect=discord_changelog.MediaError("сигнатура файла не поддерживается"),
        ):
            batch = next(iter(discord_changelog.iter_entry_media_batches(entry)))

        self.assertEqual(
            discord_changelog.RemoteMedia(
                "https://www.youtube.com/watch?v=example",
                None,
                change_index=0,
            ),
            batch[0],
        )

    def test_remote_only_batch_uses_json_request(self):
        remote = discord_changelog.RemoteMedia("https://www.youtube.com/watch?v=example", None)

        with patch.object(discord_changelog, "_send_discord_payload") as send_json, patch.object(
            discord_changelog,
            "send_multipart_discord",
        ) as send_multipart:
            discord_changelog.send_media_batch([remote], None, discord_changelog.time.monotonic() + 10)

        send_json.assert_called_once()
        send_multipart.assert_not_called()

    def test_media_is_rendered_after_its_changelog_line(self):
        image = discord_changelog.DownloadedMedia(
            "image", None, b"png", "image/png", "media-1.png", change_index=0
        )
        video = discord_changelog.DownloadedMedia(
            "video", None, b"webm", "video/webm", "media-2.webm", change_index=0
        )
        second_image = discord_changelog.DownloadedMedia(
            "second", None, b"png", "image/png", "media-3.png", change_index=2
        )
        entry = {
            "author": "Tester",
            "changes": [
                {"type": "Add", "message": "Первая строка"},
                {"type": "Fix", "message": "Вторая строка"},
                {"type": "Tweak", "message": "Третья строка"},
            ],
            "url": "https://example.org/pr/1",
        }

        payload, _ = discord_changelog.build_media_payload(
            {"title": "Автор: Tester", "description": ""},
            [image, video, second_image],
            entry,
        )

        components = payload["components"][0]["components"]
        self.assertEqual("### Автор: Tester", components[0]["content"])
        self.assertEqual("🆕 Первая строка", components[1]["content"])
        self.assertEqual("attachment://media-1.png", components[2]["items"][0]["media"]["url"])
        self.assertEqual("attachment://media-2.webm", components[2]["items"][1]["media"]["url"])
        self.assertEqual({"type": 14, "divider": True, "spacing": 2}, components[3])
        self.assertEqual("🪛 Вторая строка\n⚒️ Третья строка", components[4]["content"])
        self.assertEqual("attachment://media-3.png", components[5]["items"][0]["media"]["url"])
        self.assertEqual({"type": 14, "divider": True, "spacing": 1}, components[6])
        self.assertEqual("[GitHub Pull Request](https://example.org/pr/1)", components[7]["content"])

    def test_media_batches_obey_file_count_and_request_size(self):
        def batches_for(media):
            entry = {"media": [{"url": item.url} for item in media]}
            with patch.object(discord_changelog, "download_media", side_effect=media):
                return list(discord_changelog.iter_entry_media_batches(entry))

        small = [
            discord_changelog.DownloadedMedia(str(index), None, b"x", "video/mp4", f"media-{index}.mp4")
            for index in range(11)
        ]
        batches = batches_for(small)
        self.assertEqual([10, 1], [len(batch) for batch in batches])

        item_size = discord_changelog.MEDIA_MAX_REQUEST_SIZE // 8 + 1
        large = [
            discord_changelog.DownloadedMedia(str(index), None, b"x" * item_size, "video/mp4", f"media-{index}.mp4")
            for index in range(9)
        ]
        self.assertEqual([7, 2], [len(batch) for batch in batches_for(large)])

    def test_downloaded_media_keeps_its_change_index(self):
        entry = {
            "changes": [{}, {}],
            "media": [
                {"url": "https://example.org/first.png", "change": 0},
                {"url": "https://example.org/second.webm", "change": 1},
            ],
        }
        media = [
            discord_changelog.DownloadedMedia("first", None, b"png", "image/png", "first.png"),
            discord_changelog.DownloadedMedia("second", None, b"webm", "video/webm", "second.webm"),
        ]

        with patch.object(discord_changelog, "download_media", side_effect=media):
            batch = next(iter(discord_changelog.iter_entry_media_batches(entry)))

        self.assertEqual([0, 1], [item.change_index for item in batch])

    def test_invalid_change_record_is_skipped_when_collecting_media(self):
        output = io.StringIO()

        with patch.object(discord_changelog, "download_media") as download, redirect_stdout(output):
            batches = list(discord_changelog.iter_entry_media_batches({"changes": ["не объект"]}))

        self.assertEqual([], batches)
        download.assert_not_called()
        self.assertIn("запись changes имеет неверный формат", output.getvalue())

    def test_media_count_and_total_deadline_are_bounded(self):
        records = [
            {"url": f"https://example.org/{index}.png"}
            for index in range(discord_changelog.MEDIA_MAX_FILES_PER_ENTRY + 1)
        ]
        media = discord_changelog.DownloadedMedia("url", None, b"png", "image/png", "media.png")

        with patch.object(discord_changelog, "download_media", return_value=media) as download:
            batches = list(discord_changelog.iter_entry_media_batches({"media": records}))

        self.assertEqual(discord_changelog.MEDIA_MAX_FILES_PER_ENTRY, sum(map(len, batches)))
        self.assertEqual(discord_changelog.MEDIA_MAX_FILES_PER_ENTRY, download.call_count)

        with self.assertRaises(discord_changelog.DiscordPublishTimeoutError):
            discord_changelog.download_media(
                "https://example.org/image.png",
                deadline=discord_changelog.time.monotonic() - 1,
            )

    def test_multipart_payload_and_rate_limit_retry(self):
        payload = {"embeds": [{"description": "Текст"}]}
        files = [("files[0]", ("media.png", b"png", "image/png"))]
        limited = Mock(status_code=429)
        limited.json.return_value = {"retry_after": 0}
        sent = Mock(status_code=204)

        with patch.object(discord_changelog.requests, "post", side_effect=[limited, sent]) as post, patch.object(
            discord_changelog.time, "sleep"
        ):
            discord_changelog.send_multipart_discord(
                payload,
                files,
                deadline=discord_changelog.time.monotonic() + 10,
            )

        self.assertEqual(
            {"payload_json": json.dumps(payload, ensure_ascii=False)},
            post.call_args_list[0].kwargs["data"],
        )
        self.assertEqual(files, post.call_args_list[0].kwargs["files"])

    def test_components_v2_payload_enables_webhook_components(self):
        payload = {"flags": discord_changelog.DISCORD_COMPONENTS_V2_FLAG, "components": []}

        with patch.object(discord_changelog, "DISCORD_WEBHOOK_URL", "https://discord.example/webhook"), patch.object(
            discord_changelog.requests, "post", return_value=Mock(status_code=204)
        ) as post:
            discord_changelog.send_multipart_discord(
                payload,
                [],
                deadline=discord_changelog.time.monotonic() + 10,
            )

        self.assertEqual("https://discord.example/webhook?with_components=true", post.call_args.args[0])

    def test_media_send_failure_falls_back_to_text_and_warns(self):
        entry = {
            "author": "Tester",
            "changes": [{"type": "Add", "message": "Добавлено"}],
            "media": [{"url": "https://example.org/image.png"}],
        }
        media = discord_changelog.DownloadedMedia(
            entry["media"][0]["url"], None, b"png", "image/png", "media-1.png"
        )
        output = io.StringIO()

        with patch.object(discord_changelog, "DISCORD_WEBHOOK_URL", "https://discord.example/webhook"), patch.object(
            discord_changelog, "download_media", return_value=media
        ), patch.object(
            discord_changelog, "send_multipart_discord", side_effect=RuntimeError("Discord недоступен")
        ) as multipart, patch.object(discord_changelog, "send_embed_discord") as text_send, redirect_stdout(output):
            discord_changelog.send_to_discord([entry])

        multipart.assert_called_once()
        text_send.assert_called_once()
        self.assertIn("::warning::Медиа https://example.org/image.png", output.getvalue())

    def test_images_and_videos_are_sent_with_changelog_in_source_order(self):
        image = discord_changelog.DownloadedMedia(
            "https://example.org/image.png", None, b"png", "image/png", "media-1.png"
        )
        video = discord_changelog.DownloadedMedia(
            "https://example.org/video.webm", None, b"webm", "video/webm", "media-2.webm"
        )
        entry = {
            "author": "Tester",
            "changes": [{"type": "Add", "message": "Добавлено"}],
        }

        with patch.object(discord_changelog, "DISCORD_WEBHOOK_URL", "https://discord.example/webhook"), patch.object(
            discord_changelog,
            "iter_entry_media_batches",
            return_value=iter([[image, video]]),
        ), patch.object(discord_changelog, "send_media_batch") as send, patch.object(
            discord_changelog,
            "send_embed_discord",
        ) as send_text:
            discord_changelog.send_to_discord([entry])

        send.assert_called_once()
        self.assertEqual([image, video], send.call_args.args[0])
        self.assertIsNotNone(send.call_args_list[0].args[1])
        send_text.assert_not_called()

    def test_split_text_with_media_uses_only_the_selected_text_part(self):
        media = discord_changelog.DownloadedMedia(
            "https://example.org/image.png", None, b"png", "image/png", "media-1.png"
        )
        entry = {"author": "Tester", "changes": [{"type": "Add", "message": "Добавлено"}]}

        with patch.object(discord_changelog, "DISCORD_WEBHOOK_URL", "https://discord.example/webhook"), patch.object(
            discord_changelog,
            "split_message",
            return_value=["Первая часть", "Последняя часть"],
        ), patch.object(
            discord_changelog,
            "iter_entry_media_batches",
            return_value=iter([[media], [media]]),
        ), patch.object(discord_changelog, "send_media_batch") as send, patch.object(
            discord_changelog,
            "send_embed_discord",
        ) as send_text:
            discord_changelog.send_to_discord([entry])

        self.assertEqual("Последняя часть", send.call_args_list[0].args[1]["description"])
        self.assertEqual([None, None], [item.args[3] for item in send.call_args_list])
        send_text.assert_called_once()
        self.assertEqual("Первая часть", send_text.call_args.args[0]["description"])

    def test_all_split_text_parts_are_sent_when_there_is_no_media(self):
        entry = {"author": "Tester", "changes": [{"type": "Add", "message": "Добавлено"}]}

        with patch.object(discord_changelog, "DISCORD_WEBHOOK_URL", "https://discord.example/webhook"), patch.object(
            discord_changelog,
            "split_message",
            return_value=["Первая часть", "Последняя часть"],
        ), patch.object(
            discord_changelog,
            "iter_entry_media_batches",
            return_value=iter([]),
        ), patch.object(discord_changelog, "send_embed_discord") as send:
            discord_changelog.send_to_discord([entry])

        self.assertEqual(
            ["Первая часть", "Последняя часть"],
            [item.args[0]["description"] for item in send.call_args_list],
        )

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
        publish_workflow = PUBLISH_WORKFLOW_PATH.read_text(encoding="utf-8")
        discord_workflow = DISCORD_WORKFLOW_PATH.read_text(encoding="utf-8")
        runner = RUNNER_PATH.read_text(encoding="utf-8")
        document = yaml.load(workflow, Loader=yaml.BaseLoader)
        yaml.load(discord_workflow, Loader=yaml.BaseLoader)

        self.assertIn("on", document)
        self.assertIn("jobs", document)
        self.assertIn("pull_request_target:\n    types: [closed]\n    branches: [master]", workflow)
        self.assertIn("workflow_dispatch:", workflow)
        self.assertIn("changelog:", workflow)
        self.assertIn("type: string", workflow)
        self.assertIn(":ci: Автор", workflow)
        self.assertIn("Много строк — через gh/API", workflow)
        self.assertIn("if: github.event_name == 'workflow_dispatch' || github.event.pull_request.merged == true", workflow)
        self.assertNotIn("push:", workflow)
        self.assertNotIn("schedule:", workflow)
        self.assertNotIn("PR_NUMBER:", workflow)
        self.assertIn("MANUAL_CHANGELOG: ${{ inputs.changelog }}", workflow)
        self.assertIn("permissions:\n  actions: read", workflow)
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
        self.assertIn("ACTIONS_TOKEN: ${{ github.token }}", workflow)
        self.assertIn("CHANGELOG_FILE: ${{ vars.CHANGELOG_FILE }}", workflow)
        self.assertNotIn("actions_changelogs_since_last_run.py", publish_workflow)
        self.assertIn('workflows: ["Publish Stable"]', discord_workflow)
        self.assertIn("run-name: Discord changelog for ${{ github.event.workflow_run.head_sha }}", discord_workflow)
        self.assertIn("branches: [stable]", discord_workflow)
        self.assertIn("github.event.workflow_run.conclusion == 'success'", discord_workflow)
        self.assertIn("ref: ${{ github.event.workflow_run.head_sha }}", discord_workflow)
        self.assertIn("SOURCE_WORKFLOW_RUN_ID: ${{ github.event.workflow_run.id }}", discord_workflow)
        self.assertIn("CHANGELOG_FILE: ${{ vars.CHANGELOG_FILE }}", discord_workflow)
        self.assertIn("GITHUB_TOKEN: ${{ github.token }}", discord_workflow)
        self.assertIn("group: publish-discord-changelog", discord_workflow)
        self.assertIn("persist-credentials: false", discord_workflow)
        self.assertIn("actions_changelogs_since_last_run.py", discord_workflow)
        self.assertNotIn("CHANGELOG_TOKEN", workflow)
        self.assertNotIn("CHANGELOG_SSH_KEY", workflow)
        self.assertIn("concurrency:", workflow)
        self.assertIn("cancel-in-progress: false", workflow)
        self.assertIn("run: bash Tools/_sunrise/changelog/run.sh", workflow)
        self.assertNotIn("git reset --hard origin/master", workflow)
        self.assertIn("git reset --hard origin/master", runner)
        self.assertIn("for attempt in {1..5}", runner)
        self.assertIn("python Tools/_sunrise/changelog/changelog_actions.py", runner)
        self.assertIn('arguments+=(--manual-changelog)', runner)
        self.assertIn('if [[ -z "${MANUAL_CHANGELOG:-}" ]]', runner)
        self.assertNotIn("changelog-state.json", runner)
        self.assertIn('git commit -m "Automatic changelog update [skip ci]"', runner)
        self.assertIn('if [[ -z "${CHANGELOG_FILE:-}" ]]', runner)
        self.assertIn('changelog_directory="$(dirname -- "$CHANGELOG_FILE")"', runner)
        self.assertIn('"$category" == *".."*', runner)
        self.assertIn('! "$category" =~ ^[A-Za-z]+$', runner)
        self.assertIn('changelog_files+=("$changelog_directory/$category.yml")', runner)
        self.assertIn('git add -- "${changelog_files[@]}"', runner)
        self.assertIn("git add -A -- Resources/Changelog/Parts", runner)
        self.assertNotIn('git add -- "$changelog_directory"', runner)
        self.assertIn("Чейнджлог уже актуален: публикация не требуется.", runner)
        self.assertIn("Чейнджлог успешно опубликован в master.", runner)
        self.assertIn("Не удалось отправить чейнджлог после пяти попыток.", runner)
        self.assertNotIn("github.event.pull_request.head", workflow)

    def test_parts_staging_includes_new_file(self):
        with tempfile.TemporaryDirectory() as directory:
            repo_root = Path(directory)
            changelog = repo_root / "Resources/Changelog/ChangelogSunrise.yml"
            part = repo_root / "Resources/Changelog/Parts/new.yml"
            changelog.parent.mkdir(parents=True)
            part.parent.mkdir(parents=True)
            changelog.write_text("Entries: []\n", encoding="utf-8")
            part.write_text("changes: []\n", encoding="utf-8")

            subprocess.run(["git", "init", "--quiet"], cwd=repo_root, check=True)
            subprocess.run(
                ["git", "add", "--", "Resources/Changelog/ChangelogSunrise.yml"],
                cwd=repo_root,
                check=True,
            )
            subprocess.run(
                ["git", "add", "-A", "--", "Resources/Changelog/Parts"],
                cwd=repo_root,
                check=True,
            )
            staged = subprocess.run(
                ["git", "diff", "--cached", "--name-only"],
                cwd=repo_root,
                check=True,
                capture_output=True,
                text=True,
            ).stdout.splitlines()

        self.assertEqual(
            [
                "Resources/Changelog/ChangelogSunrise.yml",
                "Resources/Changelog/Parts/new.yml",
            ],
            staged,
        )


if __name__ == "__main__":
    unittest.main()
