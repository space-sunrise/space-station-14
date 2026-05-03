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

    def test_sync_step_prefers_pat_before_default_github_token(self):
        self.assertIn(
            "github-token: ${{ secrets.AUTO_DRAFT_TOKEN || secrets.GITHUB_TOKEN }}",
            self.workflow,
        )
        self.assertIn("AUTO_DRAFT_TOKEN_CONFIGURED: ${{ secrets.AUTO_DRAFT_TOKEN != '' }}", self.workflow)
        self.assertIn("function requireDraftMutationToken(number)", self.workflow)

    def test_auto_draft_comment_is_created_after_successful_draft_conversion(self):
        blocking_branch = re.search(
            r"if \(hasBlockingFeedback\) \{\s*if \(!pullRequest\.isDraft\) \{(?P<body>.*?)\n\s*\} else if \(hasMarker\)",
            self.workflow,
            re.DOTALL,
        )

        self.assertIsNotNone(blocking_branch)
        body = blocking_branch.group("body")
        require_token_position = body.index("requireDraftMutationToken(number);")
        convert_position = body.index("await convertToDraft(pullRequest.id);")
        label_position = body.index("await addLabel(number, markerLabel);")
        comment_position = body.index("await upsertAutoDraftComment(number, blockingThreads, blockingReviews);")

        self.assertLess(require_token_position, convert_position)
        self.assertLess(convert_position, label_position)
        self.assertLess(convert_position, comment_position)


if __name__ == "__main__":
    unittest.main()
