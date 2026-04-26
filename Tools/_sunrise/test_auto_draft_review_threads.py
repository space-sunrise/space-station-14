import re
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
WORKFLOW_PATH = REPO_ROOT / ".github" / "workflows" / "auto-draft-review-threads.yml"


class AutoDraftReviewThreadsWorkflowTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.workflow = WORKFLOW_PATH.read_text(encoding="utf-8")

    def test_ai_app_review_threads_are_recognized_by_actor_type(self):
        self.assertIn('AI_REVIEWER_APPS: "coderabbitai"', self.workflow)
        self.assertGreaterEqual(
            len(re.findall(r"author\s*\{\s*login\s*__typename\s*\}", self.workflow)),
            2,
        )
        self.assertIn("function isAiReviewer(author)", self.workflow)
        self.assertRegex(self.workflow, r"isAiReviewer\(blockingComment\??\.author\)")
        self.assertIn("isAiReviewer(review.author)", self.workflow)
        self.assertIn("!isAiReviewer(review.author)", self.workflow)


if __name__ == "__main__":
    unittest.main()
