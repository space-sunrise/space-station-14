using Content.Shared._Sunrise.Weapons.DualWield;
using Content.Client._Sunrise.UserInterface.DualWield;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;
using Robust.Shared.GameStates;

namespace Content.Client._Sunrise.Weapons.DualWield;

/// <summary>
/// Sunrise-Edit: Клиентская система для отображения индикатора режима "стрельбы по македонски".
/// Управляет видимостью иконки в правом верхнем углу.
/// </summary>
public sealed class ClientDualWieldSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    private DualWieldIndicator? _indicator;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DualWieldComponent, ComponentHandleState>(OnDualWieldHandleState);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        if (_indicator != null)
        {
            _indicator.RemoveFromParent();
            _indicator?.Dispose();
            _indicator = null;
        }
    }

    private void OnDualWieldHandleState(Entity<DualWieldComponent> ent, ref ComponentHandleState args)
    {
        // Проверяем что это наш персонаж (игрок)
        var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;
        if (playerEntity != ent.Owner)
            return;

        if (args.Current is not DualWieldComponent state)
            return;

        // Создаём индикатор если его нет
        if (_indicator == null)
        {
            _indicator = new DualWieldIndicator();

            // Позиционирование в правом верхнем углу
            _indicator.SetAnchorsPreset(Control.LayoutPreset.TopRight);
            _indicator.MarginLeft = -150;
            _indicator.MarginTop = 20;
            _indicator.MarginRight = -10;
            _indicator.MarginBottom = 160;

            _uiManager.StateRoot.AddChild(_indicator);
        }

        // Обновляем видимость индикатора
        _indicator.SetActive(state.Active);
    }
}
