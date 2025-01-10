using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Ghost;

[Serializable, NetSerializable]
public enum AcceptGhostUiButton
{
    Deny,
    Accept,
}

[Serializable, NetSerializable]
public sealed class AcceptGhostChoiceMessage : EuiMessageBase
{
    public readonly AcceptGhostUiButton Button;

    public AcceptGhostChoiceMessage(AcceptGhostUiButton button)
    {
        Button = button;
    }
}
