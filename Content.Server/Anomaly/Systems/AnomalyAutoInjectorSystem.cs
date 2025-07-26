using Content.Shared.Anomaly.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Server.Audio;
using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;
using System.Collections.Generic;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Anomaly.Systems;

public sealed partial class AnomalyAutoInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyAutoInjectorComponent, AfterInteractEvent>(OnAfterInteract);
        // MapInitEvent больше не нужен
    }

    // Все возможные инжекторные аномалии (AnomalyTrap*)
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

    private void OnAfterInteract(EntityUid uid, AnomalyAutoInjectorComponent comp, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        // Если инъектор уже использован — попап и выход
        if (_entMan.HasComponent<UsedAnomalyAutoInjectorComponent>(uid))
        {
            _popup.PopupEntity("Нечего вводить!", uid, args.User);
            return;
        }

        // Проверяем, что цель — живой моб
        if (!_entMan.HasComponent<MobStateComponent>(target))
            return;

        // Если цель уже заражена — попап и выход
        if (_entMan.HasComponent<InnerBodyAnomalyComponent>(target))
        {
            _popup.PopupEntity("Кожа не поддается введению", target, args.User);
            return;
        }

        // Выбираем случайный прототип инжекторной аномалии
        var random = IoCManager.Resolve<IRobustRandom>();
        var protoId = AllAnomalyTrapProtos[random.Next(AllAnomalyTrapProtos.Count)];
        if (!_proto.TryIndex<EntityPrototype>(protoId, out var protoTrap))
            return;
        // Получаем injectionComponents из InnerBodyAnomalyInjectorComponent в protoTrap
        var injectorCompData = protoTrap.Components.Values.FirstOrDefault(c => c.Component is Content.Shared.Anomaly.Components.InnerBodyAnomalyInjectorComponent);
        if (injectorCompData == null)
            return;
        var injectionComponents = ((Content.Shared.Anomaly.Components.InnerBodyAnomalyInjectorComponent)injectorCompData.Component).InjectionComponents;
        _entMan.AddComponents(target, injectionComponents);

        // Добавляем компонент, помечающий как использованный
        _entMan.AddComponent<UsedAnomalyAutoInjectorComponent>(uid);

        // Проигрываем звук напрямую (жестко задаём путь к звуку, как в YAML)
        _audio.PlayPvs("/Audio/Items/hypospray.ogg", uid);

        args.Handled = true;
    }
}
