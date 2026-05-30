using Content.Server.Light.EntitySystems;
using Content.Server._Sunrise.Shadegen.Components;
using Content.Shared._Sunrise.Shadegen.Components;
using Content.Shared.Light.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Shadegen;

public sealed class ShadegenSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HandheldLightSystem _handheldLight = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _updateQueue = [];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadegenComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime < component.NextUpdate)
                continue;

            component.NextUpdate = _timing.CurTime + component.UpdateCooldown;

            foreach (var toUpdate in _updateQueue)
            {
                if (!Deleted(toUpdate))
                    RemComp<ShadegenAffectedComponent>(toUpdate);
            }

            _updateQueue.Clear();

            var lights = _lookup.GetEntitiesInRange<PointLightComponent>(Transform(uid).Coordinates, component.Range);
            foreach (var light in lights)
            {
                if (HasComp<DarkLightComponent>(light.Owner))
                    continue;

                EnsureComp<ShadegenAffectedComponent>(light.Owner);
                _updateQueue.Add(light.Owner);

                if (TryComp<HandheldLightComponent>(light.Owner, out var handheld) && handheld.Activated)
                    _handheldLight.TurnOff((light.Owner, handheld), makeNoise: false);
            }
        }
    }
}
