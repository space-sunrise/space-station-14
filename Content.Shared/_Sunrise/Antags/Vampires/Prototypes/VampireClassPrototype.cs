using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Antags.Vampires.Prototypes;

/// <summary>
/// Data-driven определение подкласса вампира.
///
/// Для добавления нового класса вампира нужно только:
/// - новый компонент класса
/// - система для этого компонента
/// - запись <see cref="VampireClassPrototype"/> в YAML
/// </summary>
[Prototype]
public sealed partial class VampireClassPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField(required: true)]
    public string Tooltip { get; private set; } = default!;
    [DataField(required: true)]
    public SpriteSpecifier Icon { get; private set; } = default!;
    [DataField(required: true)]
    public string ClassComponent { get; private set; } = default!;
    [DataField]
    public List<EntProtoId> Actions { get; private set; } = new();
}
