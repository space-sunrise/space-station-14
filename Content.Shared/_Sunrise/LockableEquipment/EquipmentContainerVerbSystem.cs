using Content.Shared.Containers;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Log;

namespace Content.Shared._Sunrise.LockableEquipment;

/// <summary>
/// Builds locally available lockable-equipment interaction verbs from replicated state.
/// </summary>
public sealed class EquipmentContainerVerbSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly LayerAccessSystem _layerAccess = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EquipmentContainerComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<EquipmentContainerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var device = GetEquipment(ent.Owner, ent.Comp);
        if (device == null)
        {
            Log.Info($"lockable-verbs: no installed device for {ToPrettyString(ent.Owner)}.");
            return;
        }

        if (!TryComp(device.Value, out LockableEquipmentComponent? comp))
        {
            Log.Info($"lockable-verbs: missing lockable component on {ToPrettyString(device.Value)}.");
            return;
        }

        if (!CanAccess(ent.Owner, comp.Layer, comp))
        {
            Log.Info($"lockable-verbs: access blocked for {ToPrettyString(ent.Owner)} layer={comp.Layer}.");
            return;
        }

        var name = MetaData(device.Value).EntityName;
        TryComp(args.User, out HandsComponent? hands);

        if (hands != null)
        {
            var addedKeyVerb = false;
            var addedBreakVerb = false;

            foreach (var hand in _hands.EnumerateHands(args.User))
            {
                if (!_hands.TryGetHeldItem(args.User, hand, out var held))
                    continue;

                if (!addedKeyVerb && !comp.Broken && HasComp<KeyComponent>(held.Value))
                {
                    addedKeyVerb = true;

                    args.Verbs.Add(new InteractionVerb
                    {
                        Text = comp.Locked
                            ? Loc.GetString("lockable-equipment-verb-unlock", ("name", name))
                            : Loc.GetString("lockable-equipment-verb-lock", ("name", name)),
                        EventTarget = ent,
                        ExecutionEventArgs = new EquipmentContainerUseHeldKeyVerbEvent(args.User)
                    });
                }

                if (!addedBreakVerb && comp.Locked && CanBreakWithTool(held.Value, comp))
                {
                    var breakText = GetBreakVerbText(name, comp.Mode);
                    if (breakText == null)
                        continue;

                    addedBreakVerb = true;
                    args.Verbs.Add(new InteractionVerb
                    {
                        Text = breakText,
                        EventTarget = ent,
                        ExecutionEventArgs = new EquipmentContainerBreakWithHeldToolVerbEvent(args.User)
                    });
                }
            }
        }

        if (!comp.Locked)
        {
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString("lockable-equipment-verb-remove", ("name", name)),
                EventTarget = ent,
                ExecutionEventArgs = new EquipmentContainerRemoveVerbEvent(args.User)
            });
        }

        Log.Info($"lockable-verbs: built verbs for {ToPrettyString(ent.Owner)} device={ToPrettyString(device.Value)} user={ToPrettyString(args.User)}.");
    }

    private EntityUid? GetEquipment(EntityUid uid, EquipmentContainerComponent comp)
    {
        if (!_container.TryGetContainer(uid, comp.ContainerId, out var container))
            return null;

        return FindDevice(container);
    }

    private EntityUid? FindDevice(BaseContainer container)
    {
        foreach (var ent in container.ContainedEntities)
        {
            if (HasComp<LockableEquipmentComponent>(ent))
                return ent;
        }

        return null;
    }

    private bool CanAccess(EntityUid owner, string layer, LockableEquipmentComponent device)
    {
        return _layerAccess.IsLayerAccessible(owner, layer, device);
    }

    private bool CanBreakWithTool(EntityUid tool, LockableEquipmentComponent comp)
    {
        return _tag.HasTag(tool, comp.RequiredToolTag);
    }

    private string? GetBreakVerbText(string name, LockableEquipmentComponent.BreakMode mode)
    {
        return mode switch
        {
            LockableEquipmentComponent.BreakMode.ForceOpen =>
                Loc.GetString("lockable-equipment-verb-force-open", ("name", name)),

            LockableEquipmentComponent.BreakMode.Breakable =>
                Loc.GetString("lockable-equipment-verb-break", ("name", name)),

            LockableEquipmentComponent.BreakMode.Destroyable =>
                Loc.GetString("lockable-equipment-verb-destroy", ("name", name)),

            _ => null
        };
    }
}
