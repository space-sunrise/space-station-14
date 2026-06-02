using Content.Shared._Sunrise.Antags.Vampires.Systems;
using Content.Shared._Sunrise.Antags.Vampires.Components;
using Content.Shared._Sunrise.Antags.Vampires.Components.Abilities;
using Content.Shared._Sunrise.Antags.Vampires.Components.Effects;
using Content.Shared._Sunrise.Antags.Vampires.Components.Visuals;
using Content.Shared.Physics;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared._Sunrise.Antags.Vampires.Systems.Abilities;

public abstract class SharedSanguinePoolSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SanguinePoolComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<SanguinePoolComponent> ent, ref PreventCollideEvent args)
    {
        if (HasComp<MapGridComponent>(args.OtherEntity))
            return;

        var otherLayer = (CollisionGroup) args.OtherFixture.CollisionLayer;
        if ((otherLayer & CollisionGroup.WallLayer) != 0)
            return;

        args.Cancelled = true;
    }
}
