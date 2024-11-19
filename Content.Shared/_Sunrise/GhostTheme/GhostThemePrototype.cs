using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.GhostTheme;

[Prototype("ghostTheme")]
public sealed class GhostThemePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("components")]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; } = new();
}
