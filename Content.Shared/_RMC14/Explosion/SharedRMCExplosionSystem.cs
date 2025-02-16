namespace Content.Shared._RMC14.Explosion;

public sealed class SharedRMCExplosionSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<CMExplosionEffectComponent, CMExplosiveTriggeredEvent>(OnExplosionEffectTriggered);
    }

    private void OnExplosionEffectTriggered(Entity<CMExplosionEffectComponent> ent, ref CMExplosiveTriggeredEvent args)
    {
        DoEffect(ent);
    }

    public void DoEffect(Entity<CMExplosionEffectComponent> ent)
    {
        if (ent.Comp.ShockWave is { } shockwave)
            SpawnNextToOrDrop(shockwave, ent);

        if (ent.Comp.Explosion is { } explosion)
            SpawnNextToOrDrop(explosion, ent);
    }

    public void TryDoEffect(Entity<CMExplosionEffectComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        DoEffect((ent, ent.Comp));
    }
}

[ByRefEvent]
public readonly record struct CMExplosiveTriggeredEvent;

