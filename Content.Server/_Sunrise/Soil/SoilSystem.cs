﻿using Content.Server.Movement.Systems;
using Content.Server.Popups;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;

namespace Content.Server._Sunrise.Soil;

/// <summary>
/// Система для мешка с землей
/// </summary>
public sealed class SoilSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SoilComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<SoilComponent> ent, ref UseInHandEvent args)
    {
        var xform = Transform(ent);
        var grid = _transform.GetGrid((ent.Owner, xform));
        if (grid == null)
        {
            _popup.PopupEntity(Loc.GetString(ent.Comp.PopupStringFailed), ent, args.User, PopupType.SmallCaution);
            return;
        }

        var spawned = _entity.SpawnEntity(ent.Comp.SpawnPrototype, xform.Coordinates);

        var targetMeta = MetaData(ent);
        var userMeta = MetaData(args.User);

        _popup.PopupEntity(Loc.GetString(ent.Comp.PopupStringSuccess, ("user", userMeta.EntityName), ("name", targetMeta.EntityName)), args.User, PopupType.LargeGreen);
        _stamina.TryTakeStamina(args.User, ent.Comp.StaminaDamage);

        _entity.QueueDeleteEntity(ent.Owner);
    }
}
