using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Messenger;

/// <summary>
/// Прототип эмодзи для мессенджера
/// </summary>
[Prototype("emoji")]
public sealed partial class EmojiPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

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
