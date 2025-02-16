using Content.Shared._RMC14.Explosion.Components;
using Content.Shared.Explosion.Components;
using Robust.Shared.Spawners;

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
        {
            var wave = SpawnNextToOrDrop(shockwave, ent);
            ModifyShockwave(ent, wave);
        }

        if (ent.Comp.Explosion is { } explosion)
            SpawnNextToOrDrop(explosion, ent);
    }

    private void ModifyShockwave(Entity<CMExplosionEffectComponent> ent, EntityUid wave)
    {
        if (!TryComp<ExplosiveComponent>(ent, out var explosionComponent))
            return;

        // Дальше идут просто числа, которые я придумал особо не думая, мб нужно подумать
        // Но идея в том, чтобы чем сильнее взрыв, тем сильнее эффект и наоборот
        // TODO: Разобраться, почему каждый взрыв все равно создает разную волну, даже с отключенной этой системой

        if (TryComp<RMCExplosionShockWaveComponent>(wave, out var waveComponent))
        {
            waveComponent.FalloffPower ??= explosionComponent.TotalIntensity / 4f;
            waveComponent.Width ??= explosionComponent.TotalIntensity / 200f;

            Dirty(wave, waveComponent);
        }

        if (TryComp<TimedDespawnComponent>(wave, out var timedDespawnComponent))
            timedDespawnComponent.Lifetime = Math.Clamp(explosionComponent.TotalIntensity / 50f, 0.1f, 0.8f);
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

