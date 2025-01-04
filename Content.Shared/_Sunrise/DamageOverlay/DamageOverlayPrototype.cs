using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.DamageOverlay;

[Prototype]
public sealed partial class DamageOverlayPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = string.Empty;

    [DataField]
    public HashSet<string> Types = new();

    [DataField]
    public bool StructureDamageEnabled = true;

    [DataField]
    public bool ToPlayerDamageEnabled = true;
}
