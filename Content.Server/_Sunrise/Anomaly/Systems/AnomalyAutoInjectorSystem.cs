// СУТЬ: заразить цель случайной аномалией с 2 этапами (заражение - эффект глюков и начало таймера, превращение - конец таймера, урон и добавление компонента аномалии), блокирует повторное использование, меняет спрайт и выводит попапы.
// ГОВНОКОД: звук иньекции воспроизводится напрямую, но зато метаболизма нет :)
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
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyAutoInjectorComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private bool IsAlreadyInfected(EntityUid target) => _entMan.HasComponent<InnerBodyAnomalyComponent>(target);
    private bool IsPendingInfection(EntityUid target) => _entMan.HasComponent<PendingAnomalyInfectionComponent>(target);
    private bool IsInjectorUsed(EntityUid injector) => _entMan.HasComponent<UsedAnomalyAutoInjectorComponent>(injector);
    private bool IsHumanoid(EntityUid target) => _entMan.HasComponent<HumanoidAppearanceComponent>(target);
    private bool IsMob(EntityUid target) => _entMan.HasComponent<MobStateComponent>(target);

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

    private void OnAfterInteract(EntityUid uid, Component comp, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;
        if (!IsMob(target))
            return;
        if (comp is not AnomalyAutoInjectorComponent injectorComp)
            return;
        if (!IsValidTargetForInjection(target, uid, injectorComp, out var popup))
        {
            if (popup != null)
                ShowPopup(popup, target, args.User);
            return;
        }
        _entMan.AddComponent<PendingAnomalyInfectionComponent>(target);
        _entMan.AddComponent<UsedAnomalyAutoInjectorComponent>(uid);
        if (_entMan.EntityExists(uid) && _entMan.HasComponent<TransformComponent>(uid))
            _audio.PlayPvs(injectorComp.HypospraySound, uid);
        args.Handled = true;
        EnsureComp<StatusEffectsComponent>(target);
        var statusSys = EntitySystem.Get<StatusEffectsSystem>();
        statusSys.TryAddStatusEffect(target, injectorComp.RainbowEffect, TimeSpan.FromSeconds(injectorComp.RainbowDuration), false, injectorComp.RainbowEffect);
        Timer.Spawn(TimeSpan.FromSeconds(injectorComp.AnomalyDelay), () =>
        {
            var damage = new DamageSpecifier();
            damage.DamageDict["Cellular"] = injectorComp.CellularDamage;
            _damageableSystem.TryChangeDamage(target, damage);
            if (!IsAlreadyInfected(target))
                TryInfectWithRandomAnomaly(target, injectorComp);
            if (IsPendingInfection(target))
                _entMan.RemoveComponent<PendingAnomalyInfectionComponent>(target);
        });
    }

    // можно было бы отдельным файлом сделать
    private void TryInfectWithRandomAnomaly(EntityUid target, AnomalyAutoInjectorComponent comp)
    {
        if (_entMan.HasComponent<InnerBodyAnomalyComponent>(target))
            return;
        if (comp.AnomalyTrapProtos.Count == 0)
            return;
        var protoId = comp.AnomalyTrapProtos[_random.Next(comp.AnomalyTrapProtos.Count)];
        if (!_proto.TryIndex<EntityPrototype>(protoId, out var protoTrap))
            return;
        var injectorCompData = protoTrap.Components.Values.FirstOrDefault(c => c.Component is InnerBodyAnomalyInjectorComponent);
        if (injectorCompData == null || injectorCompData.Component is not InnerBodyAnomalyInjectorComponent anomalyInjector)
            return;
        var injectionComponents = anomalyInjector.InjectionComponents;
        _entMan.AddComponents(target, injectionComponents);
        if (_entMan.HasComponent<PendingAnomalyInfectionComponent>(target))
            _entMan.RemoveComponent<PendingAnomalyInfectionComponent>(target);
    }
}
