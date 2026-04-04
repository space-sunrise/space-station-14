using Content.Server.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.LockableEquipment;
using Robust.Shared.GameObjects;

namespace Content.Server._Sunrise.LockableEquipment;

public sealed class LockableEquipmentSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LockableEquipmentComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<LockableEquipmentComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInit(EntityUid uid, LockableEquipmentComponent component, ComponentInit args)
    {
        // Initialization logic if needed
    }

    private void OnInteractUsing(EntityUid uid, LockableEquipmentComponent component, InteractUsingEvent args)
    {
        if (args.Handled) return;

        // Assuming using an access card or key to toggle lock
        if (TryComp<AccessComponent>(args.Used, out var access) && _accessReader.IsAllowed(uid, args.User))
        {
            component.Locked = !component.Locked;
            args.Handled = true;
        }
    }
}