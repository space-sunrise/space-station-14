using Content.Server.Construction.Completions;
using Content.Server.Dragon;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Sunrise.DragonsBrood;

public sealed class DragonsBroodSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonsBroodComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DragonsBroodComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMobStateChanged(Entity<DragonsBroodComponent> ent, ref MobStateChangedEvent args)
    {
        if (_mobState.IsAlive(ent.Owner))
            return;

        if (!TryComp<DragonRiftComponent>(ent.Comp.MotherRift, out var dragonRift))
            return;

        RaiseLocalEvent(new DragonsBroodDeadEvent((ent.Comp.MotherRift, dragonRift)));
    }

    private void OnShutdown(Entity<DragonsBroodComponent> ent, ref ComponentShutdown args)
    {
        if (_mobState.IsAlive(ent.Owner))
            return;

        if (!TryComp<DragonRiftComponent>(ent.Comp.MotherRift, out var dragonRift))
            return;

        RaiseLocalEvent(new DragonsBroodDeadEvent((ent.Comp.MotherRift, dragonRift)));
    }
}

[Serializable]
public record struct DragonsBroodDeadEvent(Entity<DragonRiftComponent> Rift);
