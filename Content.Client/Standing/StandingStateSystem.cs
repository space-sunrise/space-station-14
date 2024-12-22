using Content.Shared.Input;
using Content.Shared.Standing;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.Standing;

public sealed class StandingStateSystem : SharedStandingStateSystem
{
    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleStanding, InputCmdHandler.FromDelegate(ToggleStanding))
            .Register<SharedStandingStateSystem>();
    }

    private void ToggleStanding(ICommonSession? session)
    {
        if (session is not { AttachedEntity: { Valid: true } uid } _
            || !Exists(uid))
            return;

        RaiseNetworkEvent(new ChangeLayingDownEvent());
    }
}
