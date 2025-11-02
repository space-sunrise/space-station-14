using Content.Server.Dragon;
using Content.Shared.Mobs;

namespace Content.Server._Sunrise.DragonsBrood;

public sealed class DragonsBroodSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonsBroodComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<DragonsBroodComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        if (!TryComp<DragonRiftComponent>(ent.Comp.MotherRift, out var dragonRift))
            return;

        RaiseLocalEvent(new DragonsBroodDeadEvent((ent.Comp.MotherRift, dragonRift)));
    }
}

public sealed class DragonsBroodDeadEvent(Entity<DragonRiftComponent> rift) : EventArgs
{
    public Entity<DragonRiftComponent> RiftComp { get; init; } = rift;
}
