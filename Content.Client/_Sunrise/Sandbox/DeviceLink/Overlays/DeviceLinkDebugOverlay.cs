using Content.Client._Sunrise.Sandbox.DeviceLink.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Sunrise.Sandbox.DeviceLink.Overlays;

public sealed class DeviceLinkDebugOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;

    private readonly DeviceLinkingVisualizationSystem _deviceLinking;
    private readonly TransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public DeviceLinkDebugOverlay()
    {
        IoCManager.InjectDependencies(this);

        _deviceLinking = _ent.System<DeviceLinkingVisualizationSystem>();
        _transform = _ent.System<TransformSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var rays = _deviceLinking.Rays;
        if (rays.Count == 0)
            return;

        var colors = _deviceLinking.SourceColors;

        foreach (var ray in rays)
        {
            if (args.Space != OverlaySpace.WorldSpace)
                continue;

            if (!_ent.TryGetComponent<TransformComponent>(ray.Key, out var sourceTransform)
                || !sourceTransform.MapUid.HasValue)
                continue;

            var rayColor = colors?.GetValueOrDefault(ray.Key);

            if (rayColor is null)
                rayColor = Color.White;

            var sourcePos = _transform.GetWorldPosition(sourceTransform);

            args.WorldHandle.DrawCircle(sourcePos, 0.1f, rayColor.Value);

            foreach (var connection in ray.Value)
            {
                if (!_ent.TryGetComponent<TransformComponent>(connection, out var destinationTransform)
                    || !destinationTransform.MapUid.HasValue)
                    continue;

                var destinationPos = _transform.GetWorldPosition(destinationTransform);

                args.WorldHandle.DrawLine(sourcePos, destinationPos, rayColor.Value);
            }
        }
    }
}
