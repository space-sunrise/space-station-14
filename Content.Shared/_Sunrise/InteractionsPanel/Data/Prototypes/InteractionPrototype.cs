using Content.Shared._Sunrise.InteractionsPanel.Data.Conditions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.InteractionsPanel.Data.Prototypes;

[Prototype]
public sealed partial class InteractionPrototype : IPrototype
{
    [IdDataField]
    [ViewVariables]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name { get; private set; } = string.Empty;

    [DataField]
    public string? Description { get; private set; }

    [DataField]
    public ProtoId<InteractionIconPrototype>? Icon { get; private set; }

    [DataField(required: true)]
    public ProtoId<InteractionCategoryPrototype> Category { get; private set; }

    [DataField]
    public List<IAppearCondition> AppearConditions { get; private set; } = new();

    [DataField(required: true)]
    public List<string> InteractionMessages { get; private set; } = new();

    [DataField]
    public List<ProtoId<InteractionSoundPrototype>> InteractionSounds { get; private set; } = new();

    [DataField]
    public bool SpawnsEffect { get; private set; }

    [DataField]
    public float EffectChance { get; private set; }

    [DataField]
    public ProtoId<InteractionEntityEffectPrototype>? EntityEffect { get; private set; }

    [DataField]
    public TimeSpan Cooldown { get; private set; }
}
