using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server._Sunrise.ReplacementVocal;

/// <summary>
/// Компонент для замены вокальных звуков и эмоций сущности.
/// Сохраняет предыдущие настройки для возможности восстановления.
/// </summary>
[RegisterComponent]
public sealed partial class ReplacementVocalComponent : Component
{
    [DataField(required: true)]
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>> Vocal;

    [DataField]
    public HashSet<string> AddedEmotes = new();

    [DataField]
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>? PreviousVocal;
}
