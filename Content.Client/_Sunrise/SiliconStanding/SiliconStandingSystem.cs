using Content.Shared._Sunrise.SiliconStanding;
using Content.Shared.Input;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.Player;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Network;

namespace Content.Client._Sunrise.SiliconStanding;

public sealed class SiliconStandingSystem : EntitySystem
{
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IInputManager _input = default!;

    public override void Initialize()
    {
        base.Initialize();

        var context = _input.Contexts.GetContext("common");
        context.AddFunction(ContentKeyFunctions.ToggleBorgRest);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleBorgRest,
                new PointerInputCmdHandler((session, coords, uid) =>
                {
                    SendToggleEvent();
                    return true;
                }))
            .Register<SiliconStandingSystem>();
    }

    /// <summary>
    /// Sends a toggle request to the server if the player controls a borg.
    /// </summary>
    private void SendToggleEvent()
    {
        var uid = _player.LocalEntity;

        if (uid == null)
            return;

        if (!HasComp<BorgChassisComponent>(uid.Value))
            return;

        RaiseNetworkEvent(new ToggleStandingEvent());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        var context = _input.Contexts.GetContext("common");
        context.RemoveFunction(ContentKeyFunctions.ToggleBorgRest);

        CommandBinds.Unregister<SiliconStandingSystem>();
    }
}
