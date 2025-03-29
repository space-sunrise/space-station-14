using Content.Shared._Sunrise.BloodCult.Items;

namespace Content.Server._Sunrise.BloodCult.Items.Components;

[RegisterComponent]
public sealed partial class TorchCultistsProviderComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Active = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("cooldown")]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ItemSelected;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextUse = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadOnly)]
    public Enum UserInterfaceKey = CultTeleporterUiKey.Key;

    [ViewVariables(VVAccess.ReadWrite), DataField("usesLeft")]
    public int UsesLeft = 3;
}
