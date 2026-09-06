import importlib.util
import os
import sys
import unittest
from pathlib import Path
from unittest.mock import Mock, patch

from validate_publish import main, validate_cdn_url


class PublishTests(unittest.TestCase):
    def test_cdn_validation_and_normalization(self):
        self.assertEqual(validate_cdn_url(" https://cdn.example/base/// "), "https://cdn.example/base/")
        self.assertEqual(validate_cdn_url("https://[::1]:443"), "https://[::1]:443/")
        for value in (
            "", "http://cdn.example", "https:///path", "https://user@cdn.example",
            "https://cdn.example?q=1", "https://cdn.example#fragment",
            "https://cdn.example:invalid", "https://cdn.example:65536", "https://[broken",
            "https://cdn.example/white space", "https://cdn.example/line\nbreak",
        ):
            with self.subTest(value=value), self.assertRaises(ValueError):
                validate_cdn_url(value)

    def test_configuration_fails_before_build(self):
        environment = {
            "ROBUST_CDN_URL": "https://cdn.example", "PUBLISH_FORK_ID": "test",
            "CURRENT_REF_NAME": "master", "CURRENT_REF_TYPE": "branch",
        }
        with patch.dict(os.environ, environment, clear=True):
            main()
            with patch.dict(os.environ, {"COMMIT_HASH": "a" * 40}):
                main()
            for invalid in (
                {"ROBUST_CDN_URL": "http://cdn.example"}, {"PUBLISH_FORK_ID": " "},
                {"COMMIT_HASH": "master"}, {"COMMIT_HASH": "abc123"},
                {"PRIVATE_COMMIT_HASH": "refs/pull/1/head"},
                {"CURRENT_REF_TYPE": "tag"}, {"CURRENT_REF_NAME": "feature"},
                {"USE_PRIVATE_CONTENT": "true"},
                {"USE_PRIVATE_CONTENT": "true", "PRIVATE_REPOSITORY": "org/private"},
            ):
                with self.subTest(invalid=invalid), patch.dict(os.environ, invalid), self.assertRaises(SystemExit):
                    main()

    def test_publish_uses_checked_out_sha_and_current_configuration(self):
        tools_path = Path(__file__).resolve().parent
        spec = importlib.util.spec_from_file_location("publish_under_test", tools_path / "publish_multi_request.py")
        module = importlib.util.module_from_spec(spec)
        session = Mock()
        selected_sha = "a" * 40
        environment = {
            "PUBLISH_TOKEN": "test-token", "ROBUST_CDN_URL": " https://cdn.example/base/ ",
            "GITHUB_SHA": "b" * 40,
        }
        with patch.dict(os.environ, environment), patch.object(sys, "path", [str(tools_path), *sys.path]), patch(
            "subprocess.check_output", return_value=selected_sha + "\n"
        ) as git:
            spec.loader.exec_module(module)
            git.assert_called_once_with(["git", "rev-parse", "HEAD"], encoding="UTF-8")

        announce_spec = importlib.util.spec_from_file_location(
            "announce_under_test", tools_path.parent / "discord_publish_announce.py"
        )
        announce = importlib.util.module_from_spec(announce_spec)
        with patch.dict(os.environ, environment), patch("subprocess.check_output", return_value=selected_sha + "\n"):
            announce_spec.loader.exec_module(announce)
        self.assertEqual(announce.VERSION, selected_sha)

        with patch.object(sys, "argv", ["publish", "--fork-id", " test "]), patch.object(
            module.requests, "Session", return_value=session
        ), patch.object(module, "get_files_to_publish", return_value=[]), patch.object(
            module, "get_engine_version", return_value="1.0"
        ):
            module.main()

        self.assertEqual(session.post.call_count, 2)
        self.assertEqual(session.post.call_args_list[0].args[0], "https://cdn.example/base/fork/test/publish/start")
        self.assertEqual(session.post.call_args_list[0].kwargs["json"]["version"], selected_sha)
        self.assertEqual(session.post.call_args_list[1].kwargs["json"]["version"], selected_sha)


if __name__ == "__main__":
    unittest.main()
