using Content.Shared.Anomaly.Components;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Server.Audio;

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

        // Проверяем, что цель — живой моб
        if (!_entMan.HasComponent<MobStateComponent>(target))
            return;

        // Проверяем, что у цели нет уже InnerBodyAnomalyComponent
        if (_entMan.HasComponent<InnerBodyAnomalyComponent>(target))
        {
            _popup.PopupEntity("Цель уже заражена аномалией!", target, args.User);
            return;
        }

        // Добавляем компоненты заражения
        _entMan.AddComponents(target, comp.InjectionComponents);

        // Проигрываем звук напрямую (жестко задаём путь к звуку, как в YAML)
        _audio.PlayPvs("/Audio/Items/hypospray.ogg", uid);

        _popup.PopupEntity("Вы заразили цель аномалией!", target, args.User);
        args.Handled = true;
    }
}
