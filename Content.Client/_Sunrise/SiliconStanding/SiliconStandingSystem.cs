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
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding,
                new PointerInputCmdHandler((session, coords, uid) =>
                {
                    var ent = _player.LocalEntity;

                    if (ent != null && HasComp<BorgChassisComponent>(ent.Value))
                    {
                        SendToggleEvent();
                        return true;
                    }

                    return false;
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

        CommandBinds.Unregister<SiliconStandingSystem>();
    }
}
