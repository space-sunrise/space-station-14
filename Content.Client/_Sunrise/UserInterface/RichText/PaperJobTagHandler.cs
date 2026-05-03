using Content.Client.Paper.UI;
using Content.Shared._Sunrise.Paperwork;

namespace Content.Client._Sunrise.UserInterface.RichText;

public sealed class PaperJobTagHandler : PaperInteractiveTagHandler
{
    public override string Name => PaperInteractiveTagParsing.JobTagName;
    protected override string FallbackLabel => Loc.GetString("paper-job-fill-button");

    protected override void HandlePress(PaperWindow paperWindow, int index)
    {
        paperWindow.OnJobPressed(index);
    }
}
