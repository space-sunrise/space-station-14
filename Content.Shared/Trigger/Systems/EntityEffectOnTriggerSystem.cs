using Content.Shared.EntityEffects;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class EntityEffectOnTriggerSystem : XOnTriggerSystem<EntityEffectOnTriggerComponent>
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    protected override void OnTrigger(Entity<EntityEffectOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        _effects.ApplyEffects(target, ent.Comp.Effects, ent.Comp.Scale, args.User); // Sunrise-Edit - сохраняем инициатора для предсказанных EntityEffect.
        args.Handled = true;
    }
}
