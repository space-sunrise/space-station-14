using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.UI;

[Serializable, NetSerializable]
public sealed class TeleportSpellEuiState : EuiStateBase
{
    public Dictionary<int, string> Runes = new();

    public List<string> Distance = new();
}

[Serializable, NetSerializable]
public sealed class TeleportSpellTargetRuneSelected : EuiMessageBase
{
    public int RuneUid;
}
