// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server._Nox.Disease.Systems;
using Content.Shared._Nox.Disease.Components;
using Content.Shared._Nox.Disease.Effects;
using Content.Shared.EntityEffects;

namespace Content.Server._Nox.Disease.Effects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DamageDiseaseEntityEffectsSystem : EntityEffectSystem<DiseaseComponent, DamageDiseaseEffect>
{
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;
    protected override void Effect(Entity<DiseaseComponent> entity, ref EntityEffectEvent<DamageDiseaseEffect> args)
    {
        // Масштабируем урон и рост резиста
        var finalDamage = args.Effect.BaseDamage * args.Scale;
        var resistanceIncrease = args.Effect.ResistanceIncrease * args.Scale;

        _diseaseSystem.ApplyMedicineDamage(
            (entity, entity.Comp),
            args.Effect.Key,
            finalDamage,
            resistanceIncrease
        );
    }
}
