using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Bed;

[RegisterComponent, NetworkedComponent]
public sealed partial class BedHealModifierClothingComponent : Component
{
    [DataField]
    public float Multiplier = 0.25f;
}
