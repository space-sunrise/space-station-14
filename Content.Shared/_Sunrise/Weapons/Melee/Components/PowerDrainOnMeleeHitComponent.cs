using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Weapons.Melee.Components;

/// <summary>
/// When attached to a melee weapon, drains power on successful melee hit.
/// Drains from a slotted power cell if present, otherwise from a direct BatteryComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PowerDrainOnMeleeHitComponent : Component
{
    /// <summary>
    /// Amount of charge to drain per successful hit (in joules).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float ChargePerHit = 0f;

    /// <summary>
    /// If true, only drain when there is at least one entity hit.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool RequireActualHit = true;
}
