// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Sunrise.Disease.Components;
using Content.Shared._Sunrise.Disease.Effects;
using Content.Shared.EntityEffects;

namespace Content.Server._Sunrise.Disease.Effects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AntisepticEntityEffectsSystem : EntityEffectSystem<MetaDataComponent, AntisepticEffect>
{
    // Логика максимально простая. Можно доработать на нанесение урона болезни внутри DiseaseContaminationComponent
    // или нанесение урона сущности с DiseaseInfectionCloudComponent.
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<AntisepticEffect> args)
    {
        // DiseaseInfectionCloudComponent пердназначен только для сущности облака
        // иначе придется изменять логику на нанесение урона сущности с DiseaseInfectionCloudComponent
        // а не удалять.
        if (HasComp<DiseaseInfectionCloudComponent>(entity.Owner))
        {
            QueueDel(entity.Owner);
            return;
        }

        RemComp<DiseaseContaminationComponent>(entity.Owner);
    }
}