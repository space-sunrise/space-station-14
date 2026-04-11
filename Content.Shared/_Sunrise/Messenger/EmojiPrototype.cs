using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Sunrise.Messenger;

/// <summary>
/// Прототип эмодзи для мессенджера
/// </summary>
[Prototype("emoji")]
public sealed partial class EmojiPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<EmojiPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Короткий код эмодзи (например, ":smile:")
    /// </summary>
    [DataField(required: true)]
    public string Code { get; private set; } = default!;

    /// <summary>
    /// Путь к спрайту эмодзи
    /// </summary>
    [DataField(required: true)]
    public string SpritePath { get; private set; } = default!;

    /// <summary>
    /// Состояние спрайта
    /// </summary>
    [DataField(required: true)]
    public string SpriteState { get; private set; } = default!;
}
