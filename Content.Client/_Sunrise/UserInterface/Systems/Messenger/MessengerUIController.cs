using Content.Client.Gameplay;
using Content.Shared._Sunrise.Messenger;
using Content.Shared.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client._Sunrise.UserInterface.Systems.Messenger;

public sealed class MessengerUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenMessenger, InputCmdHandler.FromDelegate(_ => OnOpenMessenger()))
            .Register<MessengerUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<MessengerUIController>();
    }

    private void OnOpenMessenger()
    {
        var netManager = Robust.Shared.IoC.IoCManager.Resolve<Robust.Shared.GameObjects.IEntityNetworkManager>();
        netManager.SendSystemNetworkMessage(new OpenMessengerRequestEvent());
    }
}
