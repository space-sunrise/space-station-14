using Content.Client.Paper.UI;
using Content.Shared._Sunrise.Paperwork;

namespace Content.Client._Sunrise.UserInterface.RichText;

public sealed class PaperFormTagHandler : PaperInteractiveTagHandler
{
    public override string Name => PaperInteractiveTagParsing.FormTagName;
    protected override string FallbackLabel => Loc.GetString("paper-form-fill-button");

    protected override void HandlePress(PaperWindow paperWindow, int index)
    {
        paperWindow.OnFormPressed(index);
    }
}
