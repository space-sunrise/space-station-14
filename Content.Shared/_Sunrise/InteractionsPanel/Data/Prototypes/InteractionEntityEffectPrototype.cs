using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Prototypes;

[Prototype]
public sealed partial class InteractionEntityEffectPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public EntProtoId EntityEffect { get; private set; } = default!;
}
