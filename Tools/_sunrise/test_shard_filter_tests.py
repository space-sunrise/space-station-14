import importlib.util
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "Tools" / "test_shard_filter.py"
SPEC = importlib.util.spec_from_file_location("test_shard_filter", SCRIPT_PATH)
SHARD_FILTER = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(SHARD_FILTER)


class TestShardFilterTests(unittest.TestCase):
    def test_parses_localized_test_list_header(self):
        tests = SHARD_FILTER.parse_tests(
            [
                "Доступны следующие тесты:",
                "    Content.Tests.Fixture.Test",
            ]
        )

        self.assertEqual(tests, ["Content.Tests.Fixture.Test"])

    def test_groups_identically_named_methods_by_fixture(self):
        groups = SHARD_FILTER.extract_groups(
            [
                "Content.Tests.FirstFixture.Test",
                "Content.Tests.SecondFixture.Test",
                "Content.Tests.SecondFixture.Test(1)",
            ]
        )

        self.assertEqual(
            groups,
            {
                ("Content.Tests.FirstFixture", "Test"): 1,
                ("Content.Tests.SecondFixture", "Test"): 2,
            },
        )

    def test_builds_exact_fixture_and_method_filter(self):
        expression = SHARD_FILTER.build_filter(
            [
                ("Content.Tests.FirstFixture", "Test"),
                ("Content.Tests.SecondFixture", "Test"),
            ]
        )

        self.assertEqual(
            expression,
            "(class=='Content.Tests.FirstFixture'&&method=='Test')||"
            "(class=='Content.Tests.SecondFixture'&&method=='Test')",
        )

    def test_supports_legacy_method_only_discovery(self):
        groups = SHARD_FILTER.extract_groups(["Test", "ParameterizedTest(1)"])

        self.assertEqual(groups, {("", "Test"): 1, ("", "ParameterizedTest"): 1})
        self.assertEqual(
            SHARD_FILTER.build_filter(groups),
            "method=='ParameterizedTest'||method=='Test'",
        )

    def test_builds_runsettings_with_escaped_filter(self):
        settings = SHARD_FILTER.build_runsettings("class=='Fixture'&&method=='Test'")

        self.assertIn("<DisplayName>FullName</DisplayName>", settings)
        self.assertIn("<MapWarningTo>Failed</MapWarningTo>", settings)
        self.assertIn(
            "<Where>class=='Fixture'&amp;&amp;method=='Test'</Where>",
            settings,
        )


if __name__ == "__main__":
    unittest.main()
