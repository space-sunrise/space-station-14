using System.IO;
using System.Linq;
using System.Numerics;
using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Content.Client.Viewport;
using Content.Shared.Mobs.Components;
using Robust.Client.Graphics;
using Robust.Client.State;
using Robust.Shared.Network;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.CartridgeLoader.Cartridges;

public sealed class PhotoCartridgeClientSystem : EntitySystem
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private TimeSpan _nextCaptureTime = TimeSpan.Zero;
    public bool CameraReady => _timing.CurTime >= _nextCaptureTime;
    public float CaptureDistance { get; set; } = 2.0f;

    private ISawmill _sawmill = default!;

    private const int TargetPhotoWidth = 256;
    private const int TargetPhotoHeight = 256;

    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill("photo.cartridge.client");
        _netManager.RegisterNetMessage<PdaPhotoCaptureMessage>(accept: NetMessageAccept.Server);
    }

    public override void Shutdown()
    {
    }

    public Vector2 GetCameraPosition(EntityUid source, float distance)
    {
        if (!_entityManager.TryGetComponent<TransformComponent>(source, out var xform))
            return Vector2.Zero;

        var sourcePos = _transformSystem.GetWorldPosition(source);

        var lookRotation = _transformSystem.GetWorldRotation(source);
        var ignoreEnt = source;

        if (xform.ParentUid.IsValid() && !_entityManager.HasComponent<MapComponent>(xform.ParentUid) && !_entityManager.HasComponent<MapGridComponent>(xform.ParentUid))
        {
            lookRotation = _transformSystem.GetWorldRotation(xform.ParentUid);
            ignoreEnt = xform.ParentUid;
        }

        var gridRot = Angle.Zero;
        if (xform.GridUid != null)
            gridRot = _transformSystem.GetWorldRotation(xform.GridUid.Value);

        var relativeRot = lookRotation - gridRot;
        var directionVec = gridRot.RotateVec(relativeRot.GetCardinalDir().ToVec());

        var ray = new CollisionRay(sourcePos, directionVec, (int) Content.Shared.Physics.CollisionGroup.Opaque);

        var results = _physics.IntersectRayWithPredicate(xform.MapID, ray, distance, uid =>
        {
            return uid == source ||
                   uid == ignoreEnt ||
                   _entityManager.HasComponent<MobStateComponent>(uid);
        }, false).ToList();

        if (results.Count > 0)
        {
            return sourcePos + directionVec * Math.Max(0.1f, results[0].Distance - 0.2f);
        }

        return sourcePos + directionVec * distance;
    }

    public void CaptureAndSendPhoto(EntityUid source)
    {
        if (_timing.CurTime < _nextCaptureTime)
            return;

        _nextCaptureTime = _timing.CurTime + TimeSpan.FromSeconds(1.5);

        Timer.Spawn(500, () => CaptureInternal(source));
    }

    private void CaptureInternal(EntityUid source)
    {
        if (!_netManager.IsConnected)
        {
            _sawmill.Warning("Cannot capture photo: client not connected to server");
            return;
        }

        if (_playerManager.LocalPlayer?.ControlledEntity is not { } player)
            return;

        if (_stateManager.CurrentState is not IMainViewportState viewportState || viewportState.Viewport is not Control control)
            return;

        var viewport = viewportState.Viewport.Viewport;
        var sourcePos = _transformSystem.GetWorldPosition(source);

        var targetPos = GetCameraPosition(source, CaptureDistance);
        var targetScreen = _eyeManager.WorldToScreen(targetPos);

        var sourceScreen = _eyeManager.WorldToScreen(sourcePos);
        var offsetScreen = _eyeManager.WorldToScreen(sourcePos + new Vector2(1, 0));
        var logicalPixelsPerMeter = (offsetScreen - sourceScreen).Length();

        viewport.Screenshot(img => {
            var scaleX = (float)img.Width / control.Size.X;
            var scaleY = (float)img.Height / control.Size.Y;

            var texturePixelsPerMeter = logicalPixelsPerMeter * scaleX;
            var size = (int)(3.0f * texturePixelsPerMeter);

            var localTargetLogical = targetScreen - control.GlobalPosition;
            var centerX = localTargetLogical.X * scaleX;
            var centerY = localTargetLogical.Y * scaleY;

            var x = (int)(centerX - size / 2f);
            var y = (int)(centerY - size / 2f);

            x = Math.Clamp(x, 0, img.Width);
            y = Math.Clamp(y, 0, img.Height);
            var w = Math.Clamp(size, 1, img.Width - x);
            var h = Math.Clamp(size, 1, img.Height - y);

            var finalRect = new SixLabors.ImageSharp.Rectangle(x, y, w, h);
            TakeScreenshot(img, finalRect);
        });
    }

    private void TakeScreenshot<T>(Image<T> screenshot, SixLabors.ImageSharp.Rectangle cropRect) where T : unmanaged, IPixel<T>
    {
        try
        {
            ProcessCapturedImage(screenshot, cropRect);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Error processing screenshot: {ex.Message}");
        }
        finally
        {
            screenshot.Dispose();
        }
    }

    private void ProcessCapturedImage<T>(Image<T> image, SixLabors.ImageSharp.Rectangle cropRect) where T : unmanaged, IPixel<T>
    {
        var processed = image.Clone(ctx =>
        {
            ctx.Crop(cropRect);
            ctx.Resize(TargetPhotoWidth, TargetPhotoHeight);
        });

        var width = processed.Width;
        var height = processed.Height;

        byte[] imageData;
        using (var memoryStream = new MemoryStream())
        {
            processed.SaveAsPng(memoryStream);
            imageData = memoryStream.ToArray();
        }

        processed.Dispose();
        SendPhotoToServer(imageData, width, height);
    }

    private void SendPhotoToServer(byte[] imageData, int width, int height)
    {
        if (!_netManager.IsConnected)
            return;

        var message = new PdaPhotoCaptureMessage
        {
            ImageData = imageData,
            Width = width,
            Height = height
        };

        _netManager.ClientSendMessage(message);
    }
}
