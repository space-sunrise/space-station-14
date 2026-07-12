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

    /// <summary>
    /// Refreshes an open body selection UI after controller state is received from the server.
    /// </summary>
    private void OnControllerState(Entity<StationAiBodyControllerComponent> stationAi, ref AfterAutoHandleStateEvent args)
    {
        if (_ui.TryGetOpenUi<StationAiBodyBoundUserInterface>((EntityUid) stationAi, StationAiBodyUiKey.Key, out var bui))
            bui.Update();
    }
}
