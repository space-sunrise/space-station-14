using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.CartridgeLoader.Cartridges;

/// <summary>
/// Состояние UI фото-картриджа
/// </summary>
[Serializable, NetSerializable]
public sealed class PhotoUiState : BoundUserInterfaceState
{
    /// <summary>
    /// Список фотографий в галерее (PhotoId -> PhotoMetadata)
    /// </summary>
    public Dictionary<string, PhotoMetadata> Photos { get; }

    /// <summary>
    /// Статус камеры (готовность к съемке)
    /// </summary>
    public bool CameraReady { get; }

    /// <summary>
    /// Включена ли вспышка
    /// </summary>
    public bool FlashEnabled { get; }

    /// <summary>
    /// Сообщение об ошибке (если есть)
    /// </summary>
    public string? ErrorMessage { get; }

    public PhotoUiState(
        Dictionary<string, PhotoMetadata> photos,
        bool cameraReady,
        bool flashEnabled,
        string? errorMessage = null)
    {
        Photos = photos;
        CameraReady = cameraReady;
        FlashEnabled = flashEnabled;
        ErrorMessage = errorMessage;
    }
}
