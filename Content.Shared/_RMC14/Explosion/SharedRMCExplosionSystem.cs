using Content.Shared._RMC14.Explosion.Components;
using Content.Shared._Sunrise.Helpers;
using Content.Shared.Explosion.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Shared._RMC14.Explosion;

public abstract class SharedRMCExplosionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    // TODO: Зависимость этих значений от силы взрыва
    private const float MinSmokeCountPer100 = 15f;
    private const float MaxSmokeCountPer100 = 20f;

    private const float SmokeSpawnRadiusPer100 = 2f;

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
        if (!TryComp<ExplosiveComponent>(ent, out var explosionComponent))
            return;

        if (ent.Comp.ShockWave is { } shockwave)
        {
            var wave = SpawnNextToOrDrop(shockwave, ent);
            ModifyShockwave(wave, explosionComponent);
        }

        if (ent.Comp.Explosion is { } explosion)
            SpawnNextToOrDrop(explosion, ent);

        if (ent.Comp.Smoke is { } smoke)
            CreateFancySmoke(ent, explosionComponent, smoke);
    }

    private void ModifyShockwave(EntityUid wave, ExplosiveComponent  explosionComponent)
    {
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

    private void CreateFancySmoke(Entity<CMExplosionEffectComponent> ent, ExplosiveComponent explosionComponent, EntProtoId smokeId)
    {
        var modifier = explosionComponent.TotalIntensity / 100f;

        var smokeCount = _random.NextFloat(MinSmokeCountPer100 * modifier, MaxSmokeCountPer100 * modifier);
        var coords = Transform(ent).Coordinates;
        var modifiedRadius = Math.Clamp(SmokeSpawnRadiusPer100 * modifier, 2f, 10f);

        for (var i = 0; i < smokeCount; i++)
        {
            Spawn(smokeId, coords.GetRandomInRadius(modifiedRadius));
        }
    }

    public void TryDoEffect(Entity<CMExplosionEffectComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        DoEffect((ent, ent.Comp));
    }
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ExplosionSmokeEffectComponent : Component;

[ByRefEvent]
public readonly record struct CMExplosiveTriggeredEvent;

