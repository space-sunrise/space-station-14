using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Mood;

[Prototype]
public sealed partial class MoodCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
