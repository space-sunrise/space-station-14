using Content.Shared.Mobs;
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
        if (args.OldMobState != MobState.Alive)
            return;

        RaiseLocalEvent(new DragonsBroodDeadEvent(ent));
    }

    private void OnShutdown(Entity<DragonsBroodComponent> ent, ref ComponentShutdown args)
    {
        if (!_mobState.IsAlive(ent.Owner))
            return;

        RaiseLocalEvent(new DragonsBroodDeadEvent(ent));
    }
}

[Serializable]
public record struct DragonsBroodDeadEvent(Entity<DragonsBroodComponent> Brood);
