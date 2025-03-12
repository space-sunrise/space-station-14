using System.Diagnostics.CodeAnalysis;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Station.Systems;
using Content.Server.Stunnable;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Events;
using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.SSDIndicator;
using Content.Shared.Strip.Components;
using Content.Shared.Weapons.Reflect;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server._Sunrise.BloodCult.Items.Systems;

/// <summary>
///     Система для обработки логики зеркального щита. Референс: https://youtu.be/SiFY7ek_91Y?t=330&si=GB2jxaBrhe2vG5vc
/// </summary>
public sealed partial class CultMirrorShieldSystem : EntitySystem
{
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _stations = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        // Initialize subsystems
        InitializeCommands();

        _sawmill = _log.GetSawmill("mirrorshield");

        SubscribeLocalEvent<CultMirrorShieldComponent, HitScanReflectedEvent>(OnHitScanReflected);
    }

    private void OnHitScanReflected(EntityUid uid, CultMirrorShieldComponent component, HitScanReflectedEvent args)
    {
        if (TryBreakShield(uid, args))
            return;
        TrySpawnIllusion(uid, args);
    }

    /// <summary>
    ///     Логика ломания щита
    /// </summary>
    public bool TryBreakShield(Entity<CultMirrorShieldComponent?> entity, HitScanReflectedEvent args)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        if (args.Damage == null || args.ReflectType == null)
            return false;

        var sum = CalculateDamage(args.Damage);
        _sawmill.Debug($"sum damage: {sum}");

        var chance = CalculateChance(sum, args.ReflectType.Value);

        if (_random.NextFloat() > chance)
            return false;

        BreakShield(entity);

        return true;
    }

    /// <summary>
    ///     Само ломание щита
    /// </summary>
    public void BreakShield(Entity<CultMirrorShieldComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        // Стан хозяина
        var xform = Transform(entity);
        var parent = xform.ParentUid;
        _stun.TryKnockdown(parent, entity.Comp.KnockdownDuration, true);

        // Звук ломания
        _audio.PlayPvs(entity.Comp.BreakSound, parent);

        // Попап
        _popup.PopupEntity(Loc.GetString("cultshield-broken", ("name", MetaData(entity.Owner).EntityName)),
            parent,
            PopupType.LargeCaution);

        // Удаление щита
        QueueDel(entity.Owner);
    }

    /// <summary>
    ///     Логика спавна иллюзий
    /// </summary>
    public void TrySpawnIllusion(Entity<CultMirrorShieldComponent?> entity, HitScanReflectedEvent args)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        if (_random.NextFloat() > entity.Comp.IllusionChance)
            return;

        var xform = Transform(entity);
        // Логика для иллюзий
        var owner = xform.ParentUid;
        if (TryComp<BloodCultistComponent>(owner, out var cultist))
        {
            // Если хозяин культист, то...
            CreateIllusion(owner);
        }
        else
        {
            // Если хозяин не культист, то...
            CreateIllusion(owner, false, owner);
        }
    }

    public bool CreateIllusion(EntityUid uid, bool agressive = true, EntityUid? targetAggro = null)
    {
        if (!CreateBaseIllusion(uid, out var mobUid, agressive, targetAggro))
            return false;
        if (!CloneGear(uid, mobUid.Value))
            return false;

        return true;
    }

    # region Helper functions

    public bool CloneGear(EntityUid uid, EntityUid mobUid)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(uid, out var originSlotEnumerator))
            return false;
        while (originSlotEnumerator.MoveNext(out var containerSlot))
        {
            if (containerSlot.ContainedEntity is null)
                continue;
            if (!CloneItem(containerSlot.ContainedEntity.Value, out var item))
                continue;
            EnsureComp<UnremoveableComponent>(item.Value);
            _inventory.TryEquip(mobUid, item.Value, containerSlot.ID, true, true);
        }

        foreach (var held in _hands.EnumerateHeld(uid))
        {
            if (!CloneItem(held, out var item))
                continue;
            EnsureComp<UnremoveableComponent>(item.Value);
            _hands.TryPickupAnyHand(mobUid, item.Value);
        }

        return true;
    }

    public bool CreateBaseIllusion(EntityUid uid, [NotNullWhen(true)] out EntityUid? mobUid, bool agressive = true, EntityUid? targetAggro = null)
    {
        mobUid = null;
        if (!_transform.TryGetMapOrGridCoordinates(uid, out var coords))
        {
            return false;
        }

        var stationUid = _stations.GetOwningStation(uid);

        if (!_playerMan.TryGetSessionByEntity(uid, out var session))
            return false;

        var profile = _ticker.GetPlayerProfile(session);
        mobUid = _spawning.SpawnPlayerMob(coords.Value, null, profile, stationUid);

        if (HasComp<SSDIndicatorComponent>(mobUid))
            RemComp<SSDIndicatorComponent>(mobUid.Value);
        if (HasComp<StrippableComponent>(mobUid))
            RemComp<StrippableComponent>(mobUid.Value);

        // Через 15 секунд иллюзия удаляется - Не канон, удали эту хуету
        var timedDelete = EnsureComp<TimedDespawnComponent>(mobUid.Value);
        timedDelete.Lifetime = agressive ? 10f : 15f;

        _faction.ClearFactions(mobUid.Value);
        if (agressive)
        {
            // Она должна атаковать всех не культистов
            _faction.AddFaction(mobUid.Value, "BloodCult");
        }
        else
        {
            // TODO: Она должна атаковать владельца щита
            if (targetAggro != null)
            {
                _faction.AddFaction(mobUid.Value, "Passive");
                _faction.AggroEntity(mobUid.Value, targetAggro.Value);
            }
        }
        _console.ExecuteCommand($"addnpc {mobUid.Value} SimpleHumanoidHostileCompound");

        return true;
    }

    public bool CloneItem(EntityUid origin, [NotNullWhen(true)] out EntityUid? item)
    {
        item = null;
        var meta = MetaData(origin);
        var proto = meta.EntityPrototype;
        if (proto is null)
            return false;
        item = Spawn(proto.ID);
        return true;
    }

    public FixedPoint2 CalculateDamage(DamageSpecifier damageSpecifier)
    {
        FixedPoint2 sum = 0;
        foreach (var damageDictKey in damageSpecifier.DamageDict.Keys)
        {
            damageSpecifier.DamageDict.TryGetValue(damageDictKey, out var damage);
            sum += damage;
        }

        return sum;
    }

    public FixedPoint2 CalculateChance(FixedPoint2 sum, ReflectType reflectType)
    {
        var chance = 0f;

        if (reflectType == ReflectType.Energy)
            chance = Math.Clamp((sum.Float() / 100f - 0.2f) * 3f, 0f, 0.75f);

        if (reflectType == ReflectType.NonEnergy)
            chance = Math.Clamp((sum.Float() / 100f - 0.1f) * 3f, 0f, 0.75f);

        return chance;
    }

    # endregion Helper functions
}
