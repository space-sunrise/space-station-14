using System.Linq;
using Content.Server._Sunrise.TTS;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.UserInterface;
using Content.Sunrise.Interfaces.Shared;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Silicons.Borgs;

/// <summary>
/// System that handles cyborg voice changing functionality.
/// </summary>
public sealed class BorgVoiceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ISharedSponsorsManager? _sponsorsManager;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgVoiceComponent, BorgVoiceChangeActionEvent>(OnBorgVoiceChangeAction);
        SubscribeLocalEvent<BorgVoiceComponent, ComponentStartup>(OnBorgVoiceStartup);

        // Subscribe to TTS voice transformation
        SubscribeLocalEvent<BorgVoiceComponent, TransformSpeakerVoiceEvent>(OnTransformSpeakerVoice);

        // Initialize sponsors manager
        IoCManager.Instance!.TryResolveType(out _sponsorsManager);

        // Subscribe to UI events
        Subs.BuiEvents<BorgVoiceComponent>(BorgVoiceUiKey.Key, subs =>
        {
            subs.Event<BorgVoiceChangeMessage>(OnBorgVoiceChangeMessage);
        });
    }

    private void OnBorgVoiceChangeAction(EntityUid uid, BorgVoiceComponent component, BorgVoiceChangeActionEvent args)
    {
        if (!TryComp<BorgChassisComponent>(uid, out _))
            return;

        // Open the voice selection UI
        if (!_uiSystem.HasUi(uid, BorgVoiceUiKey.Key))
            return;

        // Get the player session for the performer
        if (!_playerManager.TryGetSessionByEntity(args.Performer, out var session))
            return;

        var state = CreateVoiceChangeState(uid, component, session);
        _uiSystem.SetUiState(uid, BorgVoiceUiKey.Key, state);
        _uiSystem.OpenUi(uid, BorgVoiceUiKey.Key, session);
    }

    private void OnBorgVoiceChangeMessage(EntityUid uid, BorgVoiceComponent component, BorgVoiceChangeMessage args)
    {
        if (!TryComp<BorgChassisComponent>(uid, out _))
            return;

        // Get the player session for the actor
        if (!_playerManager.TryGetSessionByEntity(args.Actor, out var session))
            return;

        // Validate the voice prototype exists and player can use it
        if (!CanUseVoice(args.VoiceId, session))
        {
            if (!_prototypeManager.TryIndex<TTSVoicePrototype>(args.VoiceId, out var voicePrototype))
            {
                _popup.PopupEntity(Loc.GetString("borg-voice-popup-invalid"), uid, args.Actor, PopupType.MediumCaution);
                return;
            }

            if (voicePrototype.SponsorOnly)
            {
                _popup.PopupEntity(Loc.GetString("borg-voice-popup-sponsor-only"), uid, args.Actor, PopupType.MediumCaution);
                return;
            }
        }

        // Get the voice prototype for the success message
        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(args.VoiceId, out var voice))
            return;

        // Set the new voice
        component.SelectedVoiceId = args.VoiceId;
        Dirty(uid, component);

        _popup.PopupEntity(Loc.GetString("borg-voice-popup-changed", ("voice", Loc.GetString(voice.Name))), uid, args.Actor, PopupType.Medium);

        // Update UI
        var state = CreateVoiceChangeState(uid, component, session);
        _uiSystem.SetUiState(uid, BorgVoiceUiKey.Key, state);
    }

    private void OnBorgVoiceStartup(EntityUid uid, BorgVoiceComponent component, ComponentStartup args)
    {
        // Set default voice if not already set
        if (component.SelectedVoiceId == null)
        {
            var defaultVoice = _prototypeManager
                .EnumeratePrototypes<TTSVoicePrototype>()
                .FirstOrDefault(v => v.RoundStart && !v.SponsorOnly);

            if (defaultVoice != null)
            {
                component.SelectedVoiceId = defaultVoice.ID;
                Dirty(uid, component);
            }
        }
    }

    private void OnTransformSpeakerVoice(EntityUid uid, BorgVoiceComponent component, TransformSpeakerVoiceEvent args)
    {
        // Use the borg's selected voice instead of the default
        if (component.SelectedVoiceId != null)
        {
            args.VoiceId = component.SelectedVoiceId.Value;
        }
        args.Effect = component.VoiceEffect;
    }

    private BorgVoiceChangeState CreateVoiceChangeState(EntityUid uid, BorgVoiceComponent component, ICommonSession player)
    {
        var availableVoices = _prototypeManager
            .EnumeratePrototypes<TTSVoicePrototype>()
            .Where(v => v.RoundStart && CanUseVoice(v.ID, player))
            .Select(v => v.ID)
            .ToList();

        return new BorgVoiceChangeState(component.SelectedVoiceId, availableVoices);
    }

    private bool CanUseVoice(string voiceId, ICommonSession player)
    {
        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(voiceId, out var voice))
            return false;

        if (!voice.SponsorOnly)
            return true;

        if (_sponsorsManager == null)
            return true;

        return _sponsorsManager.TryGetPrototypes(player.UserId, out var allowedPrototypes) && allowedPrototypes.Contains(voiceId);
    }
}
