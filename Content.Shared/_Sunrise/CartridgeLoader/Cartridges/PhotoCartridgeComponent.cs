using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.CartridgeLoader.Cartridges;

/// <summary>
/// Компонент фото-картриджа для КПК
/// </summary>
[RegisterComponent]
public sealed partial class PhotoCartridgeComponent : Component
{
    /// <summary>
    /// Галерея фотографий пользователя (PhotoId -> PhotoMetadata)
    /// </summary>
    [ViewVariables]
    public Dictionary<string, PhotoMetadata> PhotoGallery = new();

    /// <summary>
    /// Звук срабатывания затвора
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Sunrise/camera_shot.ogg");

    /// <summary>
    /// Эффект вспышки
    /// </summary>
    [DataField]
    public EntProtoId FlashEffect = "PhotoFlashEffect";

    /// <summary>
    /// Включена ли вспышка (пользовательская настройка)
    /// </summary>
    [DataField]
    public bool FlashEnabled = true;
}

/// <summary>
/// Метаданные фотографии в галерее
/// </summary>
[Serializable, NetSerializable]
public sealed class PhotoMetadata
{
    /// <summary>
    /// Уникальный идентификатор фотографии (GUID)
    /// </summary>
    public string PhotoId { get; set; }

    /// <summary>
    /// Путь к сетевому ресурсу изображения (например, "/NetTextures/Messenger/photo_123.png")
    /// </summary>
    public string ImagePath { get; set; }

    /// <summary>
    /// Время создания фотографии (относительно начала раунда)
    /// </summary>
    public TimeSpan Timestamp { get; set; }

    public PhotoMetadata(string photoId, string imagePath, TimeSpan timestamp)
    {
        PhotoId = photoId;
        ImagePath = imagePath;
        Timestamp = timestamp;
    }
}
