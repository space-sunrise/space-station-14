using Robust.Shared.Utility;
using Robust.Client.UserInterface.RichText;
namespace Content.Client.UserInterface.RichText;

public sealed class LocTag : IMarkupTag
{
    public string Name => "loc";

    public string TextBefore(MarkupNode node)
    {
        if(node.Value.TryGetString(out string? text))
        {
            return Loc.GetString(text);
        }
        return "";
    }
}
