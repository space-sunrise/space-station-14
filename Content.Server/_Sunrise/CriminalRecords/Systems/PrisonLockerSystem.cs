using Content.Shared.Access.Components;
using Content.Shared._Sunrise.CriminalRecords.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Lock;
using Content.Shared.Storage.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CriminalRecords.Systems;

public sealed class PrisonLockerSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrisonLockerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PrisonLockerComponent, StorageOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<PrisonLockerComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<PrisonLockerComponent, LockToggledEvent>(OnLockToggled);
    }

    private void OnStartup(EntityUid uid, PrisonLockerComponent component, ComponentStartup args)
    {
        _deviceLink.EnsureSinkPorts(uid, "PrisonLockerLock", "PrisonLockerUnlock");
    }

    public void LockLocker(EntityUid uid, string accessId)
    {
        if (TryComp<AccessReaderComponent>(uid, out var reader))
        {
            _accessReader.SetActive((uid, reader), true);

            if (TryComp<PrisonLockerComponent>(uid, out var locker))
            {
                if (!string.IsNullOrEmpty(locker.AccessId))
                    _accessReader.TryRemoveAccess((uid, reader), locker.AccessId);

                var accessProto = new ProtoId<AccessLevelPrototype>(accessId);
                _accessReader.TryAddAccess((uid, reader), accessProto);
                locker.AccessId = accessId;
            }
        }
    }

    public void UnlockLocker(EntityUid uid, string accessId)
    {
        // Sunrise-Edit: Lockers should remain restricted to the prisoner even after sentence ends.
        // They will be reset by the next LockLocker call when a new prisoner is incarcerated.
    }

    private void OnSignalReceived(EntityUid uid, PrisonLockerComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == "PrisonLockerLock")
        {
            if (TryComp<AccessReaderComponent>(uid, out var reader))
                _accessReader.SetActive((uid, reader), true);
        }
        else if (args.Port == "PrisonLocker_Unlock")
        {
            // For now, we don't allow remote unlocking of the entire access reader.
            // Security can always open it locally.
        }
    }

    private void OnOpenAttempt(EntityUid uid, PrisonLockerComponent component, ref StorageOpenAttemptEvent args)
    {
        component.LastUser = args.User;
    }

    private void OnLockToggled(EntityUid uid, PrisonLockerComponent component, ref LockToggledEvent args)
    {
        if (args.Locked || string.IsNullOrEmpty(component.AccessId) || args.User == null)
            return;

        var user = args.User.Value;
        var items = _accessReader.FindPotentialAccessItems(user);
        var accessToRemove = new ProtoId<AccessLevelPrototype>(component.AccessId);

        foreach (var item in items)
        {
            if (!TryComp<AccessComponent>(item, out var access))
                continue;

            if (access.Tags.Contains(accessToRemove))
            {
                QueueDel(item);
                break;
            }
        }
    }
}
