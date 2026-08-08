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
                ("Content.Tests.FirstFixture", "Test", None): 1,
                ("Content.Tests.SecondFixture", "Test", None): 2,
            },
        )

    def test_splits_large_parameterized_methods_into_individual_cases(self):
        case_count = SHARD_FILTER.PARAMETERIZED_CASE_SPLIT_THRESHOLD + 1
        tests = [f"Content.Tests.Fixture.Test({index})" for index in range(case_count)]

        groups = SHARD_FILTER.extract_groups(tests)

        self.assertEqual(len(groups), case_count)
        self.assertEqual(
            groups[("Content.Tests.Fixture", "Test", tests[0])],
            1,
        )

    def test_builds_exact_fixture_and_method_filter(self):
        expression = SHARD_FILTER.build_filter(
            [
                ("Content.Tests.FirstFixture", "Test", None),
                ("Content.Tests.SecondFixture", "Test", None),
            ]
        )

        self.assertEqual(
            expression,
            "(class=='Content.Tests.FirstFixture'&&method=='Test')||"
            "(class=='Content.Tests.SecondFixture'&&method=='Test')",
        )

    def test_builds_exact_parameterized_case_filter(self):
        expression = SHARD_FILTER.build_filter(
            [
                (
                    "Content.Tests.Fixture",
                    "Test",
                    "Content.Tests.Fixture.Test('value')",
                ),
            ]
        )

        self.assertEqual(
            expression,
            "test=='Content.Tests.Fixture.Test(\\'value\\')'",
        )

    def test_supports_legacy_method_only_discovery(self):
        groups = SHARD_FILTER.extract_groups(["Test", "ParameterizedTest(1)"])

        self.assertEqual(
            groups,
            {
                ("", "Test", None): 1,
                ("", "ParameterizedTest", None): 1,
            },
        )
        self.assertEqual(
            SHARD_FILTER.build_filter(groups),
            "method=='ParameterizedTest'||method=='Test'",
        )

    def test_balances_individual_cases_across_shards(self):
        groups = {
            ("Content.Tests.Fixture", "Test", f"Content.Tests.Fixture.Test({index})"): 1
            for index in range(4)
        }

        shards, loads = SHARD_FILTER.distribute_groups(groups, 2)

        self.assertEqual(loads, [2.0, 2.0])
        self.assertEqual([len(shard) for shard in shards], [2, 2])

    def test_builds_runsettings_with_escaped_filter(self):
        settings = SHARD_FILTER.build_runsettings("class=='Fixture'&&method=='Test'")

        self.assertIn("<DisplayName>FullName</DisplayName>", settings)
        self.assertIn("<MapWarningTo>Failed</MapWarningTo>", settings)
        self.assertIn("<NumberOfTestWorkers>1</NumberOfTestWorkers>", settings)
        self.assertIn(
            "<Where>class=='Fixture'&amp;&amp;method=='Test'</Where>",
            settings,
        )


if __name__ == "__main__":
    unittest.main()
