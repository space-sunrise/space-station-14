using Content.Shared._Sunrise.Jump;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Input;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.Jump;

public sealed partial class JumpSystem : SharedJumpSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private TimeSpan _lastJumpTime;
    private static TimeSpan _jumpCooldown;

    [ValidatePrototypeId<EmotePrototype>]
    private const string EmoteJumpProto = "Jump";

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.Jump, InputCmdHandler.FromDelegate(Jump, handle: false, outsidePrediction: false))
            .Register<JumpSystem>();

        _cfg.OnValueChanged(SunriseCCVars.JumpSoundEnable, OnJumpSoundEnabledOptionChanged, true);
        _cfg.OnValueChanged(SunriseCCVars.JumpCooldown, OnJumpCooldownChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.UnsubValueChanged(SunriseCCVars.JumpSoundEnable, OnJumpSoundEnabledOptionChanged);
    }

    private void OnJumpCooldownChanged(float value)
    {
        _jumpCooldown = TimeSpan.FromSeconds(value);
    }

    private void OnJumpSoundEnabledOptionChanged(bool option)
    {
        RaiseNetworkEvent(new ClientOptionJumpSoundEvent(option));
    }

    private void Jump(ICommonSession? session)
    {
        if (session is not { AttachedEntity: { Valid: true } uid } _
            || !Exists(uid))
            return;

        var currentTime = _gameTiming.CurTime;
        if (currentTime - _lastJumpTime < _jumpCooldown)
            return;

        _lastJumpTime = currentTime;
        RaisePredictiveEvent(new PlayEmoteMessage(EmoteJumpProto));
    }
}
