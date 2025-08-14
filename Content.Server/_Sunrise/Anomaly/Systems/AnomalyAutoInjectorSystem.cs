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
using Content.Shared.StatusEffectNew;
using Content.Shared.Damage;
using Robust.Shared.Log;
using Robust.Shared.Audio.Systems;
using Content.Shared.Humanoid;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server._Sunrise.Anomaly.Systems;

public sealed partial class AnomalyAutoInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnomalyAutoInjectorComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private bool IsAlreadyInfected(EntityUid target) => HasComp<InnerBodyAnomalyComponent>(target);
    private bool IsPendingInfection(EntityUid target) => HasComp<PendingAnomalyInfectionComponent>(target);
    private bool IsInjectorUsed(EntityUid injector) => HasComp<UsedAnomalyAutoInjectorComponent>(injector);
    private bool IsHumanoid(EntityUid target) => HasComp<HumanoidAppearanceComponent>(target);
    private bool IsMob(EntityUid target) => HasComp<MobStateComponent>(target);

    // ShowPopup wrapper removed per review

    private bool IsValidTargetForInjection(EntityUid target, EntityUid injector, AnomalyAutoInjectorComponent comp, [NotNullWhen(false)] out string? popup)
    {
        popup = null;

        if (!IsMob(target))
        {
            popup = comp.PopupNotApplicable;
            return false;
        }

        if (!IsHumanoid(target))
        {
            if (!IsInjectorUsed(injector))
                popup = comp.PopupNotApplicable;
            else
                popup = comp.PopupNothingToInject;
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
                _popup.PopupEntity(popup, target, args.User);
            return;
        }

        var pending = EnsureComp<PendingAnomalyInfectionComponent>(target);
        EnsureComp<UsedAnomalyAutoInjectorComponent>(uid);

        if (Exists(uid))
            _audio.PlayPvs(comp.HypospraySound, uid);

        args.Handled = true;

        _statusEffects.TryAddStatusEffectDuration(target, comp.RainbowEffect, TimeSpan.FromSeconds(comp.RainbowDuration));
        pending.EndAt = _timing.CurTime + TimeSpan.FromSeconds(comp.AnomalyDelay);
        pending.CellularDamage = comp.CellularDamage;
        pending.SelectedAnomalyTrapProtoId = comp.AnomalyTrapProtos.Count > 0 ? _random.Pick(comp.AnomalyTrapProtos) : null;
    }

    private void TryInfectWithRandomAnomaly(EntityUid target, AnomalyAutoInjectorComponent comp)
    {
        if (HasComp<InnerBodyAnomalyComponent>(target))
            return;

        if (comp.AnomalyTrapProtos.Count == 0)
            return;

        var protoId = _random.Pick(comp.AnomalyTrapProtos);
        if (!TryGetInjectionComponents(protoId, out var injectionComponents))
            return;

        EntityManager.AddComponents(target, injectionComponents);

        if (HasComp<PendingAnomalyInfectionComponent>(target))
            RemComp<PendingAnomalyInfectionComponent>(target);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<PendingAnomalyInfectionComponent>();
        while (query.MoveNext(out var uid, out var pending))
        {
            if (pending.EndAt > now)
                continue;

            if (!Exists(uid))
            {
                RemCompDeferred<PendingAnomalyInfectionComponent>(uid);
                continue;
            }

            var damage = new DamageSpecifier();
            damage.DamageDict["Cellular"] = pending.CellularDamage;
            _damageableSystem.TryChangeDamage(uid, damage);

            if (!IsAlreadyInfected(uid) && pending.SelectedAnomalyTrapProtoId != null)
            {
                if (TryGetInjectionComponents(pending.SelectedAnomalyTrapProtoId, out var comps))
                    EntityManager.AddComponents(uid, comps);
            }

            RemCompDeferred<PendingAnomalyInfectionComponent>(uid);
        }
    }

    private bool TryGetInjectionComponents(string protoId, [NotNullWhen(true)] out ComponentRegistry? injectionComponents)
    {
        injectionComponents = null;

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
