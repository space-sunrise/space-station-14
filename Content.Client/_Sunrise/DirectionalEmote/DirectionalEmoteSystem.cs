using Content.Client._Sunrise.DirectionalEmote.UserInterface;
using Content.Client.Examine;
using Content.Client.Popups;
using Content.Shared._Sunrise.DirectionalEmote;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.DirectionalEmote;

public sealed partial class DirectionalEmoteSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ExamineSystem _examineSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private int _maxEmoteLength;
    private float _maxEmoteDistance;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.DirectionalEmoteMaxLength, value => _maxEmoteLength = value, true);
        _cfg.OnValueChanged(SunriseCCVars.DirectionalEmoteMaxDistance, value => _maxEmoteDistance = value, true);

        SubscribeLocalEvent<DirectionalEmoteComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    public void TrySendEmote(NetEntity source, NetEntity target, string message, bool hideName = false)
    {
        if (!TryComp<DirectionalEmoteComponent>(GetEntity(source), out var sourceEmote))
            return;

        if (sourceEmote.LastSendAt + sourceEmote.Cooldown > _gameTiming.CurTime)
            return;

        sourceEmote.LastSendAt = _gameTiming.CurTime;

        if (message.Length > _maxEmoteLength || string.IsNullOrWhiteSpace(message))
        {
            _popupSystem.PopupCursor(Loc.GetString("directional-emote-length-error", ("limit", _maxEmoteLength)), PopupType.MediumCaution);
            return;
        }

        if (!_examineSystem.InRangeUnOccluded(GetEntity(source), GetEntity(target), _maxEmoteDistance))
        {
            _popupSystem.PopupCursor(Loc.GetString("directional-emote-too-long-popup", ("range", Math.Round(_maxEmoteDistance, 1))), PopupType.MediumCaution);
            return;
        }

        RaiseNetworkEvent(new DirectionalEmoteAttemptEvent(target, message, hideName));
    }

    private void OnGetVerbs(EntityUid uid, DirectionalEmoteComponent component, GetVerbsEvent<Verb> args)
    {
        if (!_examineSystem.InRangeUnOccluded(args.User, args.Target, _maxEmoteDistance) || args.User == uid)
            return;

        if (!component.CanReceiveEmotes ||
            !TryComp<DirectionalEmoteComponent>(args.User, out var userEmote) || !userEmote.CanSendEmotes)
            return;

        var uiController = _userInterfaceManager.GetUIController<DirectionalEmoteUIController>();

        Verb verb = new()
        {
            Text = Loc.GetString("directional-emote-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/emotes.svg.192dpi.png")),
            Act = () =>
            {
                uiController.OpenWindow(GetNetEntity(args.User), GetNetEntity(args.Target));
            },
        };

        args.Verbs.Add(verb);
    }
}
