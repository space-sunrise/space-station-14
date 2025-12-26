using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Mood;

[Prototype]
public sealed class MoodEffectPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    public string Description => Loc.GetString($"mood-effect-{ID}");

    [DataField, ValidatePrototypeId<MoodCategoryPrototype>]
    public string? Category;

    [DataField(required: true)]
    public float MoodChange;

    [DataField]
    public int Timeout;

    [DataField]
    public bool Hidden;
}
