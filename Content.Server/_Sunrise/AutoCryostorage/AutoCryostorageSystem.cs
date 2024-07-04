using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Server.Bed.Cryostorage;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Mind;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Audio.Systems;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Sunrise.AutoCryostorage;

public sealed class AutoCryostorageSystem : EntitySystem
{
    [Dependency] private readonly CryostorageSystem _cryostorageSystem = default!;
    [Dependency] private readonly SharedMindSystem Mind = default!;
    [Dependency] private readonly IGameTiming Timing = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AutoCryostorageComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<AutoCryostorageComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnPlayerAttached(EntityUid uid, AutoCryostorageComponent comp, PlayerAttachedEvent ev)
    {
        if (!comp.IsCounting)
            return;

        comp.IsCounting = false;
    }

    private void OnPlayerDetached(EntityUid uid, AutoCryostorageComponent comp, PlayerDetachedEvent ev)
    {
        if (comp.IsCounting)
            return;

        comp.IsCounting = true;
        Timer.Spawn(comp.TransferDelay, () => TransferToCryo(uid, comp));
    }

    private void TransferToCryo(EntityUid uid, AutoCryostorageComponent comp)
    {
        if (!comp.IsCounting)
            return;

        if (!HasComp<TransformComponent>(uid))
            return;

        if (!TryComp<TransformComponent>(uid, out var entityXform))
            return;

        var cryos = AllEntityQuery<CryostorageComponent>();
        var found = false;
        while (cryos.MoveNext(out var cryoUid, out var cryoComp))
        {
            if (entityXform.MapUid != Transform(cryoUid).MapUid)
                continue;

            _entMan.SpawnEntity(comp.PortalPrototype, entityXform.Coordinates);
            _audio.PlayPredicted(comp.EnterSound, entityXform.Coordinates, uid);

            var containedComp = EnsureComp<CryostorageContainedComponent>(uid);
            var delay = Mind.TryGetMind(uid, out var mindId, out var mindComp)
                ? cryoComp.GracePeriod
                : cryoComp.NoMindGracePeriod;
            containedComp.GracePeriodEndTime = Timing.CurTime + delay;
            containedComp.Cryostorage = cryoUid;
            var id = mindComp?.UserId ?? containedComp.UserId;

            _cryostorageSystem.HandleEnterCryostorage((uid, containedComp), id);

            cryos.Dispose();
            found = true;
        }

        if (!found)
        {
            Console.WriteLine($"Haven't found any cryos to insert entity {uid}");
        }
    }
}
