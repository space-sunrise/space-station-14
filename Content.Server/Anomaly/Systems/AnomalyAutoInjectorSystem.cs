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

    private const string PopupNothingToInject = "Нечего вводить!";
    private const string PopupNotApplicable = "Неприменимо!";
    private const string PopupPending = "Неприменимо! (ожидание)";
    private const string PopupInfected = "Неприменимо! (заражён)";
    private const string RainbowEffect = "SeeingRainbows";
    private const int RainbowDurationSec = 60;
    private const int AnomalyDelaySec = 60;
    private const int CellularDamage = 50;
    private const string HypospraySound = "/Audio/Items/hypospray.ogg";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyAutoInjectorComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private static readonly List<string> AllAnomalyTrapProtos = new()
    {
        "AnomalyTrapPyroclastic",
        "AnomalyTrapElectricity",
        "AnomalyTrapShadow",
        "AnomalyTrapIce",
        "AnomalyTrapFlora",
        "AnomalyTrapBluespace",
        "AnomalyTrapFlesh",
        "AnomalyTrapGravity",
        "AnomalyTrapTech",
        "AnomalyTrapRock",
        "AnomalyTrapSanta"
    };

    private bool IsAlreadyInfected(EntityUid target) => _entMan.HasComponent<InnerBodyAnomalyComponent>(target);
    private bool IsPendingInfection(EntityUid target) => _entMan.HasComponent<PendingAnomalyInfectionComponent>(target);
    private bool IsInjectorUsed(EntityUid injector) => _entMan.HasComponent<UsedAnomalyAutoInjectorComponent>(injector);
    private bool IsHumanoid(EntityUid target) => _entMan.HasComponent<HumanoidAppearanceComponent>(target);
    private bool IsMob(EntityUid target) => _entMan.HasComponent<MobStateComponent>(target);

    private void ShowPopup(string message, EntityUid target, EntityUid user)
    {
        _popup.PopupEntity(message, target, user);
    }

    private bool IsValidTargetForInjection(EntityUid target, EntityUid injector, out string? popup)
    {
        popup = null;
        if (!IsMob(target))
            return false;
        if (!IsHumanoid(target))
        {
            if (!IsInjectorUsed(injector))
                popup = PopupNotApplicable;
            return false;
        }
        if (IsInjectorUsed(injector))
        {
            popup = PopupNothingToInject;
            return false;
        }
        if (IsPendingInfection(target))
        {
            popup = PopupPending;
            return false;
        }
        if (IsAlreadyInfected(target))
        {
            popup = PopupInfected;
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
        if (!IsValidTargetForInjection(target, uid, out var popup))
        {
            if (popup != null)
                ShowPopup(popup, target, args.User);
            return;
        }
        // Только для гуманоидов, все проверки уже пройдены
        _entMan.AddComponent<PendingAnomalyInfectionComponent>(target);
        _entMan.AddComponent<UsedAnomalyAutoInjectorComponent>(uid);
        _audio.PlayPvs(HypospraySound, uid);
        args.Handled = true;
        EnsureComp<StatusEffectsComponent>(target);
        var statusSys = EntitySystem.Get<StatusEffectsSystem>();
        statusSys.TryAddStatusEffect(target, RainbowEffect, TimeSpan.FromSeconds(RainbowDurationSec), false, RainbowEffect);
        Timer.Spawn(TimeSpan.FromSeconds(AnomalyDelaySec), () =>
        {
            var damage = new DamageSpecifier();
            damage.DamageDict["Cellular"] = CellularDamage;
            _damageableSystem.TryChangeDamage(target, damage);
            if (!IsAlreadyInfected(target))
                TryInfectWithRandomAnomaly(target);
            if (IsPendingInfection(target))
                _entMan.RemoveComponent<PendingAnomalyInfectionComponent>(target);
        });
    }

    private void TryInfectWithRandomAnomaly(EntityUid target)
    {
        if (_entMan.HasComponent<InnerBodyAnomalyComponent>(target))
            return;
        var protoId = AllAnomalyTrapProtos[_random.Next(AllAnomalyTrapProtos.Count)];
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
