using Content.Shared._Sunrise.Trigger.TriggerOnActionPerformed;
using Content.Shared.Actions.Events;
using Content.Shared.Trigger;

namespace Content.Server._Sunrise.Trigger.TriggerOnActionPerformed;

/// <summary>
/// Bridges successfully performed actions into the generic trigger pipeline.
/// </summary>
public sealed class TriggerOnActionPerformedSystem : TriggerOnXSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnActionPerformedComponent, ActionPerformedEvent>(OnActionPerformed);
    }

    private void OnActionPerformed(Entity<TriggerOnActionPerformedComponent> ent, ref ActionPerformedEvent args)
    {
        Trigger.Trigger(ent, args.Performer, ent.Comp.KeyOut, predicted: false);
    }
}
