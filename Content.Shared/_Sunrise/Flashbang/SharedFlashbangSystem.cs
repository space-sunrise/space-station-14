using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Systems;
using System.Globalization;

namespace Content.Shared._Sunrise.Flashbang;

/// <summary>
/// Реализует радиальный оглушающий эффект вспышки при срабатывании триггера.
/// Применяет стан и падение с линейным спадом силы по дистанции.
/// Учитывает защиту из слотов HEAD и EARS через <see cref="GetFlashbangProtectionEvent"/>.
/// </summary>
public sealed class SharedFlashbangSystem : XOnTriggerSystem<FlashbangRadiusOnTriggerComponent>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    private HashSet<EntityUid> _entSet = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashbangProtectionComponent, GetFlashbangProtectionEvent>(OnProtectionDirect);
        SubscribeLocalEvent<FlashbangProtectionComponent, InventoryRelayedEvent<GetFlashbangProtectionEvent>>(OnProtectionRelayed);
        SubscribeLocalEvent<FlashbangProtectionComponent, ExaminedEvent>(OnProtectionExamined);
    }

    protected override void OnTrigger(Entity<FlashbangRadiusOnTriggerComponent> ent, EntityUid source, ref TriggerEvent args)
    {
        TryFlashbangArea(source, args.User, ent.Comp);
        args.Handled = true;
    }

    /// <summary>
    /// Применяет радиальный оглушающий эффект ко всем подходящим сущностям в зоне.
    /// </summary>
    /// <param name="source">Источник вспышки (эпицентр).</param>
    /// <param name="user">Сущность, активировавшая вспышку.</param>
    /// <param name="comp">Компонент с параметрами зоны.</param>
    public void TryFlashbangArea(EntityUid source, EntityUid? user, FlashbangRadiusOnTriggerComponent? comp = null)
    {
        if (!Resolve(source, ref comp, false))
            return;

        var areaAttempt = new FlashbangAreaAttemptEvent(user);
        RaiseLocalEvent(source, ref areaAttempt);
        if (areaAttempt.Cancelled)
            return;

        var sourceXform = Transform(source);
        var sourceMapPos = _transform.GetMapCoordinates(sourceXform);

        _entSet.Clear();
        _lookup.GetEntitiesInRange(sourceXform.Coordinates, comp.Range, _entSet);

        foreach (var target in _entSet)
        {
            // Пропускаем сущности без состояния моба — стены, предметы и т.п.
            if (!HasComp<MobStateComponent>(target))
                continue;

            var targetXform = Transform(target);
            var targetMapPos = _transform.GetMapCoordinates(targetXform);

            if (sourceMapPos.MapId != targetMapPos.MapId)
                continue;

            var realDistance = (sourceMapPos.Position - targetMapPos.Position).Length();

            if (!_examine.InRangeUnOccluded(source, target, comp.Range))
                continue;

            TryApplyFlashbangEffect(source, target, user, realDistance, comp);
        }
    }

    /// <summary>
    /// Вычисляет итоговую (виртуальную) дистанцию до цели с учётом защиты экипировки.
    /// Защита запрашивается событием только один раз на вызов.
    /// </summary>
    private float GetEffectiveDistance(EntityUid target, float realDistance, FlashbangRadiusOnTriggerComponent comp, out bool bypassProtection)
    {
        TryComp<FlashbangVulnerableComponent>(target, out var vulnComp);
        bypassProtection = comp.IgnoreResistances || (vulnComp?.BypassProtection ?? false);

        var protectionDistance = 0f;
        if (!bypassProtection)
        {
            var protEv = new GetFlashbangProtectionEvent { SourceRange = comp.Range };
            RaiseLocalEvent(target, protEv);
            protectionDistance = protEv.ProtectionDistance;
        }

        return realDistance + protectionDistance;
    }

    private static bool IsEffectStrengthEnough(float effectiveDistance, FlashbangRadiusOnTriggerComponent comp)
    {
        if (effectiveDistance >= comp.Range)
            return false;

        // Линейный коэффициент силы: 1 в эпицентре, 0 на краю зоны
        var t = 1f - effectiveDistance / comp.Range;
        return t >= comp.MinEffectStrength;
    }

    /// <returns>true если цель в радиусе и защита не блокирует эффект полностью.</returns>
    public bool CanApplyFlashbangEffect(EntityUid source, EntityUid target, float realDistance, FlashbangRadiusOnTriggerComponent comp)
    {
        var effectiveDistance = GetEffectiveDistance(target, realDistance, comp, out _);
        return IsEffectStrengthEnough(effectiveDistance, comp);
    }

    private void DoApplyFlashbangEffect(EntityUid source, EntityUid target, EntityUid? user, float effectiveDistance, bool bypassProtection, FlashbangRadiusOnTriggerComponent comp)
    {
        var t = 1f - effectiveDistance / comp.Range;

        var attemptEv = new FlashbangAttemptEvent(source, user, target, effectiveDistance);
        RaiseLocalEvent(target, ref attemptEv);

        if (attemptEv.Cancelled || attemptEv.Handled)
            return;

        TryComp<FlashbangVulnerableComponent>(target, out var vulnComp);
        var effectMultiplier = vulnComp?.EffectMultiplier ?? 1f;
        _stun.TryUpdateStunDuration(target, comp.StunDuration * t * effectMultiplier);
        _stun.TryKnockdown(target, comp.KnockdownDuration * t * effectMultiplier, force: bypassProtection);
    }

    public void TryApplyFlashbangEffect(EntityUid source, EntityUid target, EntityUid? user, float realDistance, FlashbangRadiusOnTriggerComponent comp)
    {
        var effectiveDistance = GetEffectiveDistance(target, realDistance, comp, out var bypassProtection);
        if (!IsEffectStrengthEnough(effectiveDistance, comp))
            return;

        DoApplyFlashbangEffect(source, target, user, effectiveDistance, bypassProtection, comp);
    }

    private void OnProtectionDirect(EntityUid uid, FlashbangProtectionComponent comp, GetFlashbangProtectionEvent args)
    {
        args.ProtectionDistance += args.SourceRange * comp.ProtectionRangeCoefficient;
    }

    private void OnProtectionRelayed(EntityUid uid, FlashbangProtectionComponent comp,
        InventoryRelayedEvent<GetFlashbangProtectionEvent> args)
    {
        args.Args.ProtectionDistance += args.Args.SourceRange * comp.ProtectionRangeCoefficient;
    }

    private void OnProtectionExamined(EntityUid uid, FlashbangProtectionComponent comp, ExaminedEvent args)
    {
        var reduction = Math.Clamp(comp.ProtectionRangeCoefficient, 0f, 1f);
        var percent = (reduction * 100f).ToString("0", CultureInfo.InvariantCulture);
        args.PushMarkup(reduction >= 1f
            ? Loc.GetString("flashbang-protection-examine-immunity")
            : Loc.GetString("flashbang-protection-examine", ("percent", percent)));
    }
}
