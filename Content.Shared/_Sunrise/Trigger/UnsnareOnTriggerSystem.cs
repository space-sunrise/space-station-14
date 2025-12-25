using System.Linq;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Systems;
using Content.Shared._Sunrise.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared._Sunrise.Trigger;

public sealed class UnsnareOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedEnsnareableSystem _ensnareable = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UnsnareOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<UnsnareOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<EnsnareableComponent>(target.Value, out var ensnareable) || !ensnareable.IsEnsnared)
            return;

        foreach (var ensnareEntity in ensnareable.Container.ContainedEntities.ToList())
        {
            if (!TryComp<EnsnaringComponent>(ensnareEntity, out var ensnaring))
                continue;

            _container.Remove(ensnareEntity, ensnareable.Container, force: true);
            ensnareable.IsEnsnared = ensnareable.Container.ContainedEntities.Count > 0;
            Dirty(target.Value, ensnareable);
            ensnaring.Ensnared = null;

            if (ensnaring.DestroyOnRemove)
                PredictedQueueDel(ensnareEntity);

            var ev = new EnsnareRemoveEvent(ensnaring.WalkSpeed, ensnaring.SprintSpeed);
            RaiseLocalEvent(target.Value, ev);
        }

        _ensnareable.UpdateAlert(target.Value, ensnareable);
        args.Handled = true;
    }
}
