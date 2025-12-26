using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.BloodCult.Items;

[RegisterComponent]
public sealed partial class CultSpellProviderComponent : Component
{
}

[Serializable, NetSerializable]
public sealed class CultSpellProviderSelectedBuiMessage : BoundUserInterfaceMessage
{
    public string ActionType;

    public CultSpellProviderSelectedBuiMessage(string actionType)
    {
        ActionType = actionType;
    }
}

[Serializable, NetSerializable]
public enum CultSpellProviderUiKey : byte
{
    Key
}
