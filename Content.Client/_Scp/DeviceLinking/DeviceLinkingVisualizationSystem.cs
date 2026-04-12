using Content.Client._Scp.DeviceLinking.Overlays;
using Content.Shared._Scp.DeviceLinking;
using Robust.Client.Graphics;
using Robust.Shared.Random;

namespace Content.Client._Scp.DeviceLinking;

public sealed class DeviceLinkingVisualizationSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public Dictionary<EntityUid, List<EntityUid>> Rays { get; } = new();
    public Dictionary<EntityUid, Color> SourceColors { get; } = new();

    private Color[] _randomRayColors = { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.LightBlue, Color.Blue, Color.Purple, Color.Pink };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DeviceLinkOverlayData>(OnDebugOverlayData);
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

    private void OnDebugOverlayData(DeviceLinkOverlayData args)
    {
        if (!_overlayMan.TryGetOverlay(out DeviceLinkDebugOverlay? overlay))
            return;

        Rays.Clear();

        foreach (var ray in args.Rays)
        {
            List<EntityUid> entities = new();

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

            if (!Rays.ContainsKey(source))
                Rays.Add(source, entities);

            if (!SourceColors.ContainsKey(source))
                SourceColors.Add(source, _random.Pick(_randomRayColors));
        }
    }
}
