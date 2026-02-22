using System.Diagnostics.CodeAnalysis;
using Content.Shared.Buckle.Components;
using Content.Shared.Humanoid;
using Content.Shared._Sunrise.CopyMachine;

namespace Content.Server._Sunrise.CopyMachine;

public sealed partial class CopyMachineSystem
{
    private readonly Dictionary<EntityUid, EntityUid> _cachedBuckledEntityByCopyMachineUid = new();

    private void OnEntityStrapped(Entity<CopyMachineComponent> ent, ref StrappedEvent args)
    {
        _cachedBuckledEntityByCopyMachineUid[ent.Owner] = args.Buckle.Owner;
        QueueUIUpdate(ent);
    }

    private void OnEntityUnstrapped(Entity<CopyMachineComponent> ent, ref UnstrappedEvent args)
    {
        if (_cachedBuckledEntityByCopyMachineUid.TryGetValue(ent.Owner, out var buckled) && buckled == args.Buckle.Owner)
            _cachedBuckledEntityByCopyMachineUid.Remove(ent.Owner);

        QueueUIUpdate(ent);
    }

    private bool TryGetBuckledEntity(EntityUid copyMachineUid, out EntityUid buckledEntityUid)
    {
        if (_cachedBuckledEntityByCopyMachineUid.TryGetValue(copyMachineUid, out buckledEntityUid))
        {
            if (!TerminatingOrDeleted(buckledEntityUid) &&
                TryComp<BuckleComponent>(buckledEntityUid, out var buckle) &&
                buckle.BuckledTo == copyMachineUid)
            {
                return true;
            }

            _cachedBuckledEntityByCopyMachineUid.Remove(copyMachineUid);
        }

        var buckleEnumerator = EntityQueryEnumerator<BuckleComponent>();
        while (buckleEnumerator.MoveNext(out var buckledUid, out var buckleComponent))
        {
            if (buckleComponent.BuckledTo != copyMachineUid)
                continue;

            buckledEntityUid = buckledUid;
            _cachedBuckledEntityByCopyMachineUid[copyMachineUid] = buckledUid;
            return true;
        }

        buckledEntityUid = default;
        return false;
    }

    private bool TryGetBuckledHumanoidAppearance(EntityUid copyMachineUid, [NotNullWhen(true)] out HumanoidAppearanceComponent? humanoidAppearance)
    {
        humanoidAppearance = null;

        if (!TryComp<StrapComponent>(copyMachineUid, out var strapComponent) || strapComponent.BuckledEntities.Count == 0)
            return false;

        if (!TryGetBuckledEntity(copyMachineUid, out var buckledEntityUid))
            return false;

        return TryComp(buckledEntityUid, out humanoidAppearance);
    }
}
