using Content.Client.Weapons.Ranged.Systems;

namespace Content.Client.Weapons.Ranged.Components;

[RegisterComponent, Access(typeof(GunSystem))]
public sealed partial class SpentAmmoVisualsComponent : Component
{
    /// <summary>
    /// Should we do "{_state}-spent" or just "spent"
    /// </summary>
    [DataField]
    public bool Suffix = true;

    [DataField]
    public string State = "base";

    // Sunrise edit start
    [DataField]
    public string? SpentState = null;

    [DataField]
    public bool RevealSpent = false;

    [DataField]
    public bool Tip = false;
    // Sunrise edit end
}

public enum AmmoVisualLayers : byte
{
    Base,
    Tip,
    // Sunrise added start - support spent visual layer
    Spent,
    // Sunrise added end
}
