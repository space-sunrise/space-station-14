using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server._Sunrise.ReplacementVocal;

/// <summary>
/// Компонент для замены вокальных звуков и эмоций сущности.
/// Сохраняет предыдущие настройки для возможности восстановления.
/// </summary>
[RegisterComponent]
public sealed partial class ReplacementVocalComponent : Component
{
    [DataField(customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<Sex, EmoteSoundsPrototype>), required: true)]
    public Dictionary<Sex, string> Vocal;

    [DataField]
    public HashSet<string> AddedEmotes = new();

    [DataField(customTypeSerializer: typeof(PrototypeIdValueDictionarySerializer<Sex, EmoteSoundsPrototype>))]
    public Dictionary<Sex, string>? PreviousVocal;
}
