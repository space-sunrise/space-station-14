using Content.Client.Paper.UI;
using Content.Shared._Sunrise.Paperwork;

namespace Content.Client._Sunrise.UserInterface.RichText;

public sealed class PaperSignatureTagHandler : PaperInteractiveTagHandler
{
    public override string Name => PaperInteractiveTagParsing.SignatureTagName;
    protected override string FallbackLabel => Loc.GetString("paper-signature-sign-button");
    protected override float MinHeightPadding => 4f;

    protected override void HandlePress(PaperWindow paperWindow, int index)
    {
        paperWindow.OnSignaturePressed(index);
    }
}
