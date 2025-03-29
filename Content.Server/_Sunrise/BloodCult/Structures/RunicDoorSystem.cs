using Content.Server.Doors.Systems;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared.Doors;
using Content.Shared.Humanoid;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Sunrise.BloodCult.Structures;

public sealed class RunicDoorSystem : EntitySystem
{
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RunicDoorComponent, BeforeDoorOpenedEvent>(OnBeforeDoorOpened);
        SubscribeLocalEvent<RunicDoorComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
    }

    private void OnBeforeDoorOpened(EntityUid uid, RunicDoorComponent component, BeforeDoorOpenedEvent args)
    {
        args.Uncancel();

        if (!args.User.HasValue)
        {
            return;
        }

        if (!Process(uid, args.User.Value))
        {
            args.Cancel();
        }
    }

    private void OnBeforeDoorClosed(EntityUid uid, RunicDoorComponent component, BeforeDoorClosedEvent args)
    {
        args.Uncancel();

        if (!args.User.HasValue)
        {
            return;
        }

        if (!Process(uid, args.User.Value))
        {
            args.Cancel();
        }
    }

    private bool Process(EntityUid airlock, EntityUid user)
    {
        if (HasComp<BloodCultistComponent>(user) || HasComp<ConstructComponent>(user))
        {
            return true;
        }

        _doorSystem.Deny(airlock);

        if (!HasComp<HumanoidAppearanceComponent>(user))
            return false;

        var direction = Transform(user).MapPosition.Position - Transform(airlock).MapPosition.Position;
        var impulseVector = direction * 7000;

        _physics.ApplyLinearImpulse(user, impulseVector);

        _stunSystem.TryParalyze(user, TimeSpan.FromSeconds(3), true);
        return false;
    }
}
