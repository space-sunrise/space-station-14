// Система для заражения цели случайной аномалией через двухэтапный процесс:
// 1. Заражение - визуальный эффект галлюцинаций и запуск таймера
// 2. Превращение - конец таймера, применение урона и добавление компонента аномалии
// дальше блокирует повторное использование, меняет спрайт и выводит попапы (звук инъекции воспроизводится напрямую)
using Content.Shared.Anomaly.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Random;
using System.Linq;
using System;
using Robust.Shared.Timing;
using Content.Shared.StatusEffect;
using Content.Shared.Damage;
using Robust.Shared.Log;
using Robust.Shared.Audio.Systems;
using Content.Shared.Humanoid;

namespace Content.Server.Anomaly.Systems;

public sealed partial class AnomalyAutoInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyAutoInjectorComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private bool IsAlreadyInfected(EntityUid target) => EntityManager.HasComponent<InnerBodyAnomalyComponent>(target);
    private bool IsPendingInfection(EntityUid target) => EntityManager.HasComponent<PendingAnomalyInfectionComponent>(target);
    private bool IsInjectorUsed(EntityUid injector) => EntityManager.HasComponent<UsedAnomalyAutoInjectorComponent>(injector);
    private bool IsHumanoid(EntityUid target) => EntityManager.HasComponent<HumanoidAppearanceComponent>(target);
    private bool IsMob(EntityUid target) => EntityManager.HasComponent<MobStateComponent>(target);

    private void ShowPopup(string message, EntityUid target, EntityUid user)
    {
        _popup.PopupEntity(message, target, user);
    }

    private bool IsValidTargetForInjection(EntityUid target, EntityUid injector, AnomalyAutoInjectorComponent comp, out string? popup)
    {
        popup = null;

        if (!IsMob(target))
            return false;

        if (!IsHumanoid(target))
        {
            if (!IsInjectorUsed(injector))
                popup = comp.PopupNotApplicable;
            return false;
        }

        if (IsInjectorUsed(injector))
        {
            popup = comp.PopupNothingToInject;
            return false;
        }

        if (IsPendingInfection(target))
        {
            popup = comp.PopupPending;
            return false;
        }

        if (IsAlreadyInfected(target))
        {
            popup = comp.PopupInfected;
            return false;
        }

        return true;
    }

    private void OnAfterInteract(EntityUid uid, AnomalyAutoInjectorComponent comp, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!IsMob(target))
            return;

        if (!IsValidTargetForInjection(target, uid, comp, out var popup))
        {
            if (popup != null)
                ShowPopup(popup, target, args.User);
            return;
        }

        EntityManager.AddComponent<PendingAnomalyInfectionComponent>(target);
        EntityManager.AddComponent<UsedAnomalyAutoInjectorComponent>(uid);

        if (EntityManager.EntityExists(uid))
            _audio.PlayPvs(comp.HypospraySound, uid);

        args.Handled = true;

        EnsureComp<StatusEffectsComponent>(target);
        _statusEffects.TryAddStatusEffect(target, comp.RainbowEffect, TimeSpan.FromSeconds(comp.RainbowDuration), false, comp.RainbowEffect);

        Timer.Spawn(TimeSpan.FromSeconds(comp.AnomalyDelay), () =>
        {
            if (!EntityManager.EntityExists(target))
                return;

            var damage = new DamageSpecifier();
            damage.DamageDict["Cellular"] = comp.CellularDamage;
            _damageableSystem.TryChangeDamage(target, damage);

            if (!IsAlreadyInfected(target))
                TryInfectWithRandomAnomaly(target, comp);

            EntityManager.RemoveComponent<PendingAnomalyInfectionComponent>(target);
        });
    }

    private void TryInfectWithRandomAnomaly(EntityUid target, AnomalyAutoInjectorComponent comp)
    {
        if (EntityManager.HasComponent<InnerBodyAnomalyComponent>(target))
            return;

        if (comp.AnomalyTrapProtos.Count == 0)
            return;

        var protoId = comp.AnomalyTrapProtos[_random.Next(comp.AnomalyTrapProtos.Count)];
        if (!TryGetInjectionComponents(protoId, out var injectionComponents))
            return;

        EntityManager.AddComponents(target, injectionComponents);

        if (EntityManager.HasComponent<PendingAnomalyInfectionComponent>(target))
            EntityManager.RemoveComponent<PendingAnomalyInfectionComponent>(target);
    }

    private bool TryGetInjectionComponents(string protoId, out ComponentRegistry injectionComponents)
    {
        injectionComponents = new ComponentRegistry();

        if (!_proto.TryIndex<EntityPrototype>(protoId, out var protoTrap))
            return false;

        InnerBodyAnomalyInjectorComponent? anomalyInjector = null;
        foreach (var compData in protoTrap.Components.Values)
        {
            if (compData.Component is InnerBodyAnomalyInjectorComponent injector)
            {
                anomalyInjector = injector;
                break;
            }
        }
        if (anomalyInjector == null)
            return false;

        injectionComponents = anomalyInjector.InjectionComponents;
        return true;
    }
}
