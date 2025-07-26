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

namespace Content.Server.Anomaly.Systems;

public sealed partial class AnomalyInjectorMedipenSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AnomalyInjectorMedipenComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, AnomalyInjectorMedipenComponent comp, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        // Если инъектор уже использован — попап и выход
        if (_entMan.HasComponent<UsedAnomalyInjectorMedipenComponent>(uid))
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

        // Добавляем компоненты заражения
        _entMan.AddComponents(target, comp.InjectionComponents);

        // Добавляем компонент, помечающий как использованный
        _entMan.AddComponent<UsedAnomalyInjectorMedipenComponent>(uid);

        // Проигрываем звук напрямую (жестко задаём путь к звуку, как в YAML)
        _audio.PlayPvs("/Audio/Items/hypospray.ogg", uid);

        args.Handled = true;
    }
}
