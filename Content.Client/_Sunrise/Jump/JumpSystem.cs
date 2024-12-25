using Content.Shared._Sunrise.Jump;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Jump;

public sealed class JumpSystem : SharedJumpSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private TimeSpan _lastJumpTime;
    private static readonly TimeSpan JumpCooldown = TimeSpan.FromSeconds(1);

    [ValidatePrototypeId<EmotePrototype>]
    private const string EmoteJumpProto = "EmoteJump";

    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Jump, InputCmdHandler.FromDelegate(Jump, handle: false, outsidePrediction: false))
            .Register<JumpSystem>();
    }

    private void Jump(ICommonSession? session)
    {
        if (session is not { AttachedEntity: { Valid: true } uid } _
            || !Exists(uid))
            return;

        var currentTime = _gameTiming.CurTime;
        if (currentTime - _lastJumpTime < JumpCooldown)
            return;

        _lastJumpTime = currentTime;
        RaisePredictiveEvent(new PlayEmoteMessage(EmoteJumpProto));
    }
}
