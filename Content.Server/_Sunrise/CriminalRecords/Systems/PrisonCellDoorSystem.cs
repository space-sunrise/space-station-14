using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared._Sunrise.CriminalRecords.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;

namespace Content.Server._Sunrise.CriminalRecords.Systems;

public sealed class PrisonCellDoorSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrisonCellDoorComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PrisonCellDoorComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnStartup(EntityUid uid, PrisonCellDoorComponent component, ComponentStartup args)
    {
        _deviceLink.EnsureSinkPorts(uid, "PrisonCellDoor_Lock", "PrisonCellDoor_Unlock");
    }

    public void LockDoor(EntityUid uid)
    {
        if (TryComp<AccessReaderComponent>(uid, out var reader))
        {
            _accessReader.SetActive((uid, reader), true);
            _accessReader.TryAddAccess((uid, reader), "Security");
        }
    }

    public void UnlockDoor(EntityUid uid)
    {
        if (TryComp<AccessReaderComponent>(uid, out var reader))
        {
            _accessReader.SetActive((uid, reader), false);
            _accessReader.TryRemoveAccess((uid, reader), "Security");
        }
    }

    private void OnSignalReceived(EntityUid uid, PrisonCellDoorComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == "PrisonCellDoor_Lock")
        {
            if (TryComp<AccessReaderComponent>(uid, out var reader))
            {
                _accessReader.SetActive((uid, reader), true);
                _accessReader.TryAddAccess((uid, reader), "Security");
            }
        }
        else if (args.Port == "PrisonCellDoor_Unlock")
        {
            if (TryComp<AccessReaderComponent>(uid, out var reader))
            {
                _accessReader.SetActive((uid, reader), false);
                _accessReader.TryRemoveAccess((uid, reader), "Security");
            }
        }
    }
}
