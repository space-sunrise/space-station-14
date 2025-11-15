using Content.Server.Construction.Completions;
using Content.Server.Dragon;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server._Sunrise.DragonsBrood;

public sealed class DragonsBroodSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonsBroodComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DragonsBroodComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMobStateChanged(Entity<DragonsBroodComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!TryComp<DragonRiftComponent>(ent.Comp.MotherRift, out var dragonRift))
            return;

        RaiseLocalEvent(new DragonsBroodDeadEvent((ent.Comp.MotherRift, dragonRift)));
    }

    private void OnShutdown(Entity<DragonsBroodComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<MobStateComponent>(ent.Owner, out var mobStateComp) && mobStateComp.CurrentState == MobState.Dead)
            return;

        if (!TryComp<DragonRiftComponent>(ent.Comp.MotherRift, out var dragonRift))
            return;

        RaiseLocalEvent(new DragonsBroodDeadEvent((ent.Comp.MotherRift, dragonRift)));
    }
}

[Serializable]
public record struct DragonsBroodDeadEvent(Entity<DragonRiftComponent> Rift);
