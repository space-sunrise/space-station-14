using Content.Client._Sunrise.Sandbox.DeviceLink.Overlays;
using Content.Shared._Sunrise.Sandbox;
using Robust.Client.Graphics;
using Robust.Shared.Random;

namespace Content.Client._Sunrise.Sandbox.DeviceLink.Systems;

public sealed class DeviceLinkingVisualizationSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public Dictionary<EntityUid, List<EntityUid>> Rays { get; } = new();
    public Dictionary<EntityUid, Color> SourceColors { get; } = new();

    private static readonly Color[] RayColors =
    [
        Color.Red,
        Color.Orange,
        Color.Yellow,
        Color.YellowGreen,
        Color.LimeGreen,
        Color.LightBlue,
        Color.Blue,
        Color.HotPink,
        Color.BlueViolet,
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DeviceLinkOverlayDataEvent>(OnDebugOverlayData);
        SubscribeNetworkEvent<DeviceLinkOverlayToggledEvent>(OnOverlayToggled);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        RemoveOverlay();
    }

    private void OnOverlayToggled(DeviceLinkOverlayToggledEvent args)
    {
        if (args.IsEnabled)
            _overlayMan.AddOverlay(new DeviceLinkDebugOverlay());
        else
            RemoveOverlay();
    }

    private void RemoveOverlay()
    {
        Rays.Clear();
        SourceColors.Clear();

        _overlayMan.RemoveOverlay<DeviceLinkDebugOverlay>();
    }

    private void OnDebugOverlayData(DeviceLinkOverlayDataEvent args)
    {
        if (!_overlayMan.HasOverlay<DeviceLinkDebugOverlay>())
            return;

        Rays.Clear();

        foreach (var ray in args.Rays)
        {
            List<EntityUid> entities = [];

            var source = GetEntity(ray.Source);

            if (!source.Valid || Transform(source).MapUid is null)
                continue;

            foreach (var connection in ray.Connections)
            {
                var entity = GetEntity(connection);

                if (!entity.Valid || Transform(entity).MapUid is null)
                    continue;

                entities.Add(entity);
            }

            if (entities.Count == 0)
                continue;

            Rays.TryAdd(source, entities);

            if (!SourceColors.ContainsKey(source))
                SourceColors.Add(source, _random.Pick(RayColors));
        }
    }
}
