using Content.Shared._Sunrise.Shadegen.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Map;

namespace Content.Client._Sunrise.Shadegen;

public sealed class ShadegenSystem : EntitySystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PointLightSystem _light = default!;

    private readonly HashSet<EntityUid> _updateQueue = [];

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var toUpdate in _updateQueue)
        {
            if (Deleted(toUpdate))
                continue;

            if (_container.TryGetContainingContainer(toUpdate, out var container) && container.OccludesLight)
                continue;

            if (TryComp<PointLightComponent>(toUpdate, out var light))
                _light.SetContainerOccluded(toUpdate, false, light);
        }

        _updateQueue.Clear();

        var query = EntityQueryEnumerator<ShadegenComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (Transform(uid).MapID == MapId.Nullspace)
                continue;

            var lights = _lookup.GetEntitiesInRange<PointLightComponent>(Transform(uid).Coordinates, component.Range);
            foreach (var light in lights)
            {
                if (light.Comp.ContainerOccluded || HasComp<DarkLightComponent>(light.Owner))
                    continue;

                _light.SetContainerOccluded(light.Owner, true, light.Comp);
                _updateQueue.Add(light.Owner);
            }
        }
    }
}
