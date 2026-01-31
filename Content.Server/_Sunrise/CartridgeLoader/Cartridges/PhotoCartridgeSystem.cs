using System.Linq;
using Content.Server.CartridgeLoader;
using Content.Server.DeviceNetwork.Systems;
using Content.Server._Sunrise;
using Content.Server._Sunrise.Messenger;
using Content.Server.Station.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Content.Shared._Sunrise.Messenger;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.CartridgeLoader.Cartridges;

public sealed class PhotoCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private readonly NetTexturesManager _netTexturesManager = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly MessengerServerSystem _messengerServer = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly SingletonDeviceNetServerSystem _singletonServer = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private ISawmill _sawmill = default!;

    private const int MaxPhotosPerUser = 50;
    private const int MaxPhotoSizeBytes = 2 * 1024 * 1024;
    private const double MinTimeBetweenCapturesSeconds = 1.0;
    private const int MaxPhotoWidth = 512;
    private const int MaxPhotoHeight = 512;

    private readonly Dictionary<ICommonSession, TimeSpan> _lastCaptureTimes = new();

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("photo.cartridge");

        SubscribeLocalEvent<PhotoCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<PhotoCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);

        _netManager.RegisterNetMessage<PdaPhotoCaptureMessage>(OnPhotoCaptureMessage, accept: NetMessageAccept.Server);
    }

    private void OnUiMessage(EntityUid uid, PhotoCartridgeComponent component, CartridgeMessageEvent args)
    {
        if (args is not PhotoUiMessageEvent photoMessage)
            return;

        var loaderUid = GetEntity(args.LoaderUid);
        if (loaderUid == EntityUid.Invalid)
            return;

        switch (photoMessage.Action)
        {
            case PhotoUiAction.CapturePhoto:
                HandleCapturePhoto(uid, component, loaderUid);
                break;
            case PhotoUiAction.SendPhotoToMessenger:
                HandleSendPhotoToMessenger(uid, component, loaderUid, photoMessage.PhotoId, photoMessage.RecipientId, photoMessage.GroupId);
                break;
            case PhotoUiAction.RequestGallery:
                UpdateUiState(uid, loaderUid, component);
                break;
            case PhotoUiAction.DeletePhoto:
                HandleDeletePhoto(uid, component, loaderUid, photoMessage.PhotoId);
                break;
            case PhotoUiAction.ToggleFlash:
                if (photoMessage.FlashEnabled.HasValue)
                {
                    component.FlashEnabled = photoMessage.FlashEnabled.Value;
                    UpdateUiState(uid, loaderUid, component);
                }
                break;
        }
    }

    private void HandleDeletePhoto(EntityUid uid, PhotoCartridgeComponent component, EntityUid loaderUid, string? photoId)
    {
        if (string.IsNullOrEmpty(photoId) || !component.PhotoGallery.TryGetValue(photoId, out var metadata))
            return;

        component.PhotoGallery.Remove(photoId);

        UpdateUiState(uid, loaderUid, component);
    }

    private void OnUiReady(EntityUid uid, PhotoCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        UpdateUiState(uid, args.Loader, component);
    }

    private void HandleCapturePhoto(EntityUid uid, PhotoCartridgeComponent component, EntityUid loaderUid)
    {
        if (component.PhotoGallery.Count >= MaxPhotosPerUser)
        {
            UpdateUiState(uid, loaderUid, component, errorMessage: Loc.GetString("photo-cartridge-limit-reached"));
            return;
        }

        if (component.FlashEnabled)
        {
            Spawn(component.FlashEffect, _transform.GetMapCoordinates(loaderUid));
        }

        _audio.PlayPvs(component.Sound, loaderUid);

        UpdateUiState(uid, loaderUid, component);
    }

    /// <summary>
    /// Обработчик сетевого сообщения с захваченным изображением от клиента
    /// </summary>
    private void OnPhotoCaptureMessage(PdaPhotoCaptureMessage msg)
    {
        if (!_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var session))
            return;

        if (_lastCaptureTimes.TryGetValue(session, out var lastCapture))
        {
            var timeSinceLastCapture = _gameTiming.CurTime - lastCapture;
            if (timeSinceLastCapture.TotalSeconds < MinTimeBetweenCapturesSeconds)
            {
                _sawmill.Debug($"Photo capture rejected: cooldown active ({timeSinceLastCapture.TotalSeconds:F2}s < {MinTimeBetweenCapturesSeconds}s)");
                return;
            }
        }

        if (msg.ImageData.Length > MaxPhotoSizeBytes)
        {
            _sawmill.Warning($"Photo capture rejected: image too large ({msg.ImageData.Length} bytes > {MaxPhotoSizeBytes} bytes)");
            return;
        }

        if (msg.Width <= 0 || msg.Height <= 0 || msg.Width > MaxPhotoWidth || msg.Height > MaxPhotoHeight)
        {
            _sawmill.Warning($"Photo capture rejected: invalid dimensions ({msg.Width}x{msg.Height})");
            return;
        }

        var pdaUid = FindPlayerPda(session);
        if (pdaUid == null)
        {
            _sawmill.Warning($"Photo capture rejected: no PDA found for player {session.Name}");
            return;
        }

        if (!_cartridgeLoader.TryGetProgram<PhotoCartridgeComponent>(pdaUid.Value, out var cartridgeUid, out var photoComponent))
        {
            _sawmill.Warning($"Photo capture rejected: photo cartridge not found in PDA {ToPrettyString(pdaUid.Value)}");
            return;
        }

        if (photoComponent.PhotoGallery.Count >= MaxPhotosPerUser)
        {
            UpdateUiState(cartridgeUid.Value, pdaUid.Value, photoComponent, errorMessage: Loc.GetString("photo-cartridge-limit-reached"));
            return;
        }

        var photoId = Guid.NewGuid().ToString();
        var imagePath = $"/NetTextures/Messenger/{photoId}.png";

        _netTexturesManager.RegisterDynamicResource(imagePath, msg.ImageData);

        var timestamp = _gameTiming.CurTime;
        var metadata = new PhotoMetadata(photoId, imagePath, timestamp);
        photoComponent.PhotoGallery[photoId] = metadata;

        _lastCaptureTimes[session] = timestamp;

        _sawmill.Info($"Photo captured from {session.Name}: {photoId}, path: {imagePath}, size: {msg.Width}x{msg.Height}, {msg.ImageData.Length} bytes");

        UpdateUiState(cartridgeUid.Value, pdaUid.Value, photoComponent);
    }

    /// <summary>
    /// Находит КПК игрока по его сессии
    /// </summary>
    private EntityUid? FindPlayerPda(ICommonSession session)
    {
        if (session.AttachedEntity == null)
            return null;

        var playerEntity = session.AttachedEntity.Value;

        if (_inventory.TryGetSlotEntity(playerEntity, "idcard", out var idCardEntity) &&
            TryComp<PdaComponent>(idCardEntity, out _))
        {
            return idCardEntity;
        }

        if (_inventory.TryGetSlotEntity(playerEntity, "belt", out var beltEntity) &&
            TryComp<PdaComponent>(beltEntity, out _))
        {
            return beltEntity;
        }

        var pdaQuery = EntityQueryEnumerator<PdaComponent>();
        while (pdaQuery.MoveNext(out var uid, out var pda))
        {
            if (pda.PdaOwner == playerEntity)
            {
                return uid;
            }
        }

        return null;
    }

    private void HandleSendPhotoToMessenger(EntityUid uid, PhotoCartridgeComponent component, EntityUid loaderUid, string? photoId, string? recipientId, string? groupId)
    {
        if (string.IsNullOrWhiteSpace(photoId))
            return;

        if (!component.PhotoGallery.TryGetValue(photoId, out var photoMetadata))
        {
            UpdateUiState(uid, loaderUid, component, errorMessage: Loc.GetString("photo-cartridge-photo-not-found"));
            return;
        }

        var messengerServer = FindMessengerServer(loaderUid);
        if (messengerServer == null)
        {
            UpdateUiState(uid, loaderUid, component, errorMessage: Loc.GetString("photo-cartridge-messenger-unavailable"));
            return;
        }

        if (!TryComp<DeviceNetworkComponent>(loaderUid, out var pdaDevice))
            return;

        var userId = pdaDevice.Address;
        if (string.IsNullOrEmpty(userId))
            return;

        var timestamp = _gameTiming.CurTime;
        var content = Loc.GetString("photo-cartridge-photo-text");

        if (!string.IsNullOrWhiteSpace(groupId))
        {
            _messengerServer.SendGroupMessageWithImage(messengerServer.Value, userId, groupId, content, photoMetadata.ImagePath, timestamp);
        }
        else if (!string.IsNullOrWhiteSpace(recipientId))
        {
            _messengerServer.SendPersonalMessageWithImage(messengerServer.Value, userId, recipientId, content, photoMetadata.ImagePath, timestamp);
        }
        else
        {
            UpdateUiState(uid, loaderUid, component, errorMessage: Loc.GetString("photo-cartridge-recipient-not-specified"));
            return;
        }

        UpdateUiState(uid, loaderUid, component);
    }

    private EntityUid? FindMessengerServer(EntityUid pdaUid)
    {
        var station = _stationSystem.GetOwningStation(pdaUid);
        if (station == null)
        {
            var xform = Transform(pdaUid);
            var mapId = xform.MapID;
            foreach (var s in _stationSystem.GetStations())
            {
                if (Transform(s).MapID == mapId)
                {
                    station = s;
                    break;
                }
            }
        }

        if (station == null)
            station = _stationSystem.GetStations().FirstOrDefault();

        if (station == null)
            return null;

        if (!_singletonServer.TryGetActiveServerAddress<MessengerServerComponent>(station.Value, out var serverAddress))
            return null;

        var serverQuery = EntityQueryEnumerator<MessengerServerComponent, DeviceNetworkComponent>();
        while (serverQuery.MoveNext(out var uid, out _, out var deviceNetwork))
        {
            if (deviceNetwork.Address == serverAddress && _singletonServer.IsActiveServer(uid))
            {
                return uid;
            }
        }
        return null;
    }

    private void UpdateUiState(EntityUid uid, EntityUid loaderUid, PhotoCartridgeComponent component, string? errorMessage = null)
    {
        var state = new PhotoUiState(
            photos: component.PhotoGallery,
            cameraReady: component.PhotoGallery.Count < MaxPhotosPerUser,
            flashEnabled: component.FlashEnabled,
            errorMessage: errorMessage
        );
        _cartridgeLoader.UpdateCartridgeUiState(loaderUid, state);
    }

    private EntityUid GetEntity(NetEntity netEntity)
    {
        return EntityManager.GetEntity(netEntity);
    }
}
