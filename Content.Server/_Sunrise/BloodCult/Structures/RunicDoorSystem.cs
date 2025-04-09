﻿using Content.Server.Doors.Systems;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Structures;
using Content.Shared.Damage;
using Content.Shared.Doors;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;

namespace Content.Server._Sunrise.BloodCult.Structures;

public sealed class RunicDoorSystem : EntitySystem
{
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;

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

        if (!Process(uid, args.User.Value, component))
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

        if (!Process(uid, args.User.Value, component))
        {
            args.Cancel();
        }
    }

    private bool Process(EntityUid airlock, EntityUid user, RunicDoorComponent component)
    {
        if (HasComp<BloodCultistComponent>(user) || HasComp<ConstructComponent>(user))
            return true;

        _doorSystem.Deny(airlock);

        var direction = _transform.GetMapCoordinates(user).Position - _transform.GetMapCoordinates(airlock).Position;
        _throwing.TryThrow(user, direction, component.ThrowSpeed, airlock, 10F);
        _damage.TryChangeDamage(user, component.Damage, origin: airlock);

        _stunSystem.TryParalyze(user, TimeSpan.FromSeconds(component.ParalyzeTime), true);
        return false;
    }
}
