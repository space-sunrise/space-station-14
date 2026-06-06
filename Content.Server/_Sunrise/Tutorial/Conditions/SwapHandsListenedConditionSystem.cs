using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.Conditions;
using Content.Shared.Hands;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.Tutorial.Conditions;

/// <summary>
/// Fires when the player switches their active hand via key binding or GUI,
/// regardless of whether any item is held.
/// </summary>
public sealed partial class SwapHandsListenedConditionSystem : EventListenedConditionSystemBase<SwapHandsListenedCondition>
{
    public override void Initialize()
    {
        base.Initialize();

        // Catch GUI hand button click (RequestSetHandEvent is a net message, not a directed entity event)
        SubscribeAllEvent<RequestSetHandEvent>(OnRequestSetHand);

        // Catch SwapHands key binding (runs on server via InputCmdHandler)
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.SwapHands,
                InputCmdHandler.FromDelegate(OnSwapHandsKey, handle: false, outsidePrediction: false))
            .Bind(ContentKeyFunctions.SwapHandsReverse,
                InputCmdHandler.FromDelegate(OnSwapHandsKey, handle: false, outsidePrediction: false))
            .Register<SwapHandsListenedConditionSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SwapHandsListenedConditionSystem>();
    }

    private void OnRequestSetHand(RequestSetHandEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (player == null || !HasComp<TutorialPlayerComponent>(player.Value))
            return;

        RecordEvent(player.Value, DefaultKey, player.Value);
    }

    private void OnSwapHandsKey(ICommonSession? session)
    {
        var player = session?.AttachedEntity;
        if (player == null || !HasComp<TutorialPlayerComponent>(player.Value))
            return;

        RecordEvent(player.Value, DefaultKey, player.Value);
    }
}
