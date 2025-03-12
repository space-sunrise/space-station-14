using Content.Server.Stunnable;
using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Events;
using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.BloodCult.Items.Systems;

/// <summary>
///     Система для обработки логики зеркального щита. Референс: https://youtu.be/SiFY7ek_91Y?t=330&si=GB2jxaBrhe2vG5vc
/// </summary>
public sealed class CultMirrorShieldSystem : EntitySystem
{
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

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
        _popup.PopupPredicted(Loc.GetString("cultshield-broken", ("name", MetaData(entity.Owner).EntityName)),
            parent,
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

        var xform = Transform(entity);
        // Логика для иллюзий
        var owner = xform.ParentUid;
        if (TryComp<BloodCultistComponent>(owner, out var cultist))
        {
            // Если хозяин культист, то...
        }
        // Если хозяин не культист, то...
    }

    # region Helper functions

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
