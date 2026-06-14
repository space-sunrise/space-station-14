using Content.Shared._Sunrise.Silicons.StationAi;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.Silicons.StationAi;

public sealed class StationAiBodySystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiBodyControllerComponent, AfterAutoHandleStateEvent>(OnControllerState);
    }

    private void OnControllerState(Entity<StationAiBodyControllerComponent> stationAi, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<StationAiBodyBoundUserInterface>(stationAi.Owner, StationAiBodyUiKey.Key, out var bui))
            bui.Update();
    }
}
