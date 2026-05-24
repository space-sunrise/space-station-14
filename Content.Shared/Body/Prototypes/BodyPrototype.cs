using Robust.Shared.Prototypes;

namespace Content.Shared.Body.Prototypes;

/// <summary>
/// Legacy body graph prototype kept for downstream body definitions that still use
/// the pre-Nubody <c>type: body</c> YAML format.
/// </summary>
[Prototype]
public sealed partial class BodyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public string Root { get; private set; } = string.Empty;

    [DataField]
    public Dictionary<string, BodyPrototypeSlot> Slots { get; private set; } = new();
}

/// <summary>
/// Legacy slot data for an old body graph prototype.
/// </summary>
[DataDefinition]
public sealed partial class BodyPrototypeSlot
{
    [DataField]
    public string? Part { get; private set; }

    [DataField]
    public HashSet<string> Connections { get; private set; } = new();

    [DataField]
    public Dictionary<string, string?> Organs { get; private set; } = new();
}
