using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Server.RoundEnd;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Communications;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Server.Communications
{
    public sealed class CommunicationsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
        [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        private const float UIUpdateInterval = 5.0f;

        public override void Initialize()
        {
            // All events that refresh the BUI
            SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
            SubscribeLocalEvent<AdditionalAlertLevelChangedEvent>(OnAdditionalAlertLevelChanged); // Sunrise-Edit
            SubscribeLocalEvent<RoundEndSystemChangedEvent>(_ => OnGenericBroadcastEvent());
            SubscribeLocalEvent<AlertLevelDelayFinishedEvent>(_ => OnGenericBroadcastEvent());

            // Messages from the BUI
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSelectAlertLevelMessage>(OnSelectAlertLevelMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSetAdditionalAlertLevelMessage>(OnSetAdditionalAlertLevelMessage); // Sunrise-Edit
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSelectAlertStationMessage>(OnSelectAlertStationMessage); // Sunrise-Edit
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleAnnounceMessage>(OnAnnounceMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleBroadcastMessage>(OnBroadcastMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleCallEmergencyShuttleMessage>(OnCallShuttleMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleRecallEmergencyShuttleMessage>(OnRecallShuttleMessage);

            // On console init, set cooldown
            SubscribeLocalEvent<CommunicationsConsoleComponent, MapInitEvent>(OnCommunicationsConsoleMapInit);

            // Sunrise-Start
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleToggleRelayMessage>(OnToggleRelayMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, ListenEvent>(OnEntitySpokeNearbyRelay);
            // Sunrise-End
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                // TODO refresh the UI in a less horrible way
                if (comp.AnnouncementCooldownRemaining >= 0f)
                {
                    comp.AnnouncementCooldownRemaining -= frameTime;
                }

                // Sunrise-Start
                if (comp.RelayCooldownRemaining > 0f)
                    comp.RelayCooldownRemaining -= frameTime;

                if (comp.IsRelaying)
                {
                    if (!this.IsPowered(uid, EntityManager))
                        StopRelay(uid, comp, announce: true);
                    else
                    {
                        comp.RelayTimeRemaining -= frameTime;
                        if (comp.RelayTimeRemaining <= 0f)
                            StopRelay(uid, comp, announce: true);
                    }
                }
                // Sunrise-End

                comp.UIUpdateAccumulator += frameTime;

                if (comp.UIUpdateAccumulator < UIUpdateInterval)
                    continue;

                comp.UIUpdateAccumulator -= UIUpdateInterval;

                if (_uiSystem.IsUiOpen(uid, CommunicationsConsoleUiKey.Key))
                    UpdateCommsConsoleInterface(uid, comp);
            }

            base.Update(frameTime);
        }

        public void OnCommunicationsConsoleMapInit(EntityUid uid, CommunicationsConsoleComponent comp, MapInitEvent args)
        {
            comp.AnnouncementCooldownRemaining = comp.InitialDelay;
            UpdateCommsConsoleInterface(uid, comp);
        }

        /// <summary>
        /// Update the UI of every comms console.
        /// </summary>
        private void OnGenericBroadcastEvent()
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                UpdateCommsConsoleInterface(uid, comp);
            }
        }

        /// <summary>
        /// Updates all comms consoles belonging to the station that the alert level was set on
        /// </summary>
        /// <param name="args">Alert level changed event arguments</param>
        private void OnAlertLevelChanged(AlertLevelChangedEvent args)
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var entStation = ResolveAlertStation(uid, comp); // Sunrise-Edit
                if (args.Station == entStation)
                    UpdateCommsConsoleInterface(uid, comp);
            }
        }

        // Sunrise added start - дополнительные коды обновляют те же консоли
        private void OnAdditionalAlertLevelChanged(AdditionalAlertLevelChangedEvent args)
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (args.Station == ResolveAlertStation(uid, comp))
                    UpdateCommsConsoleInterface(uid, comp);
            }
        }
        // Sunrise added end

        /// <summary>
        /// Updates the UI for all comms consoles.
        /// </summary>
        public void UpdateCommsConsoleInterface()
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                UpdateCommsConsoleInterface(uid, comp);
            }
        }

        /// <summary>
        /// Updates the UI for a particular comms console.
        /// </summary>
        public void UpdateCommsConsoleInterface(EntityUid uid, CommunicationsConsoleComponent comp)
        {
            var stationUid = ResolveAlertStation(uid, comp); // Sunrise-Edit
            List<string>? levels = null;
            string currentLevel = default!;
            float currentDelay = 0;
            var additionalLevels = new List<CommunicationsConsoleAdditionalAlertLevelState>(); // Sunrise-Edit
            var alertStations = comp.CanSelectAlertStation
                ? GetAlertStationStates()
                : []; // Sunrise-Edit

            if (stationUid != null)
            {
                if (TryComp(stationUid.Value, out AlertLevelComponent? alertComp) &&
                    alertComp.AlertLevels != null)
                {
                    if (alertComp.IsSelectable || comp.ForceAlertLevelChanges) // Sunrise-Edit
                    {
                        levels = new();
                        foreach (var (id, detail) in alertComp.AlertLevels.Levels)
                        {
                            if (!detail.IsAdditional && IsAlertLevelAllowed(comp, id, detail)) // Sunrise-Edit
                            {
                                levels.Add(id);
                            }
                        }

                        if (levels.Count == 0)
                            levels = null;
                    }

                    // Sunrise added start - консоль показывает только разрешённые для неё дополнительные коды
                    foreach (var (id, detail) in alertComp.AlertLevels.Levels)
                    {
                        if (!detail.IsAdditional || !IsAlertLevelAllowed(comp, id, detail))
                            continue;

                        var enabled = alertComp.ActiveAdditionalLevels.Contains(id);
                        additionalLevels.Add(new CommunicationsConsoleAdditionalAlertLevelState(
                            id,
                            enabled,
                            alertComp.CurrentDelay <= 0 && _alertLevelSystem.CanSetAdditionalLevel(
                                (stationUid.Value, alertComp),
                                id,
                                !enabled,
                                comp.ForceAlertLevelChanges)));
                    }
                    // Sunrise added end

                    currentLevel = alertComp.CurrentLevel;
                    currentDelay = _alertLevelSystem.GetAlertLevelDelay(stationUid.Value, alertComp);
                }
            }

            var canRelay = comp.RelayCooldownRemaining <= 0f && !comp.IsRelaying && this.IsPowered(uid, EntityManager); // Sunrise-Edit
            _uiSystem.SetUiState(uid, CommunicationsConsoleUiKey.Key, new CommunicationsConsoleInterfaceState(
                CanAnnounce(comp),
                CanCallOrRecall(comp),
                levels,
                currentLevel,
                currentDelay,
                _roundEndSystem.ExpectedCountdownEnd,
                // Sunrise-Start
                canRelay,
                comp.IsRelaying,
                MathF.Max(0f, comp.RelayCooldownRemaining),
                MathF.Max(0f, comp.RelayTimeRemaining),
                additionalAlertLevels: additionalLevels,
                alertStations: alertStations,
                selectedAlertStation: comp.CanSelectAlertStation && stationUid != null
                    ? GetNetEntity(stationUid.Value)
                    : null // Sunrise-Edit
                // Sunrise-End
            ));
        }

        // Sunrise added start - привилегированная консоль может управлять кодами станции на другой карте
        private EntityUid? ResolveAlertStation(EntityUid console, CommunicationsConsoleComponent component)
        {
            var owningStation = _stationSystem.GetOwningStation(console);
            if (!component.CanSelectAlertStation)
                return owningStation != null && IsValidAlertStation(owningStation.Value)
                    ? owningStation
                    : null;

            if (component.SelectedAlertStation is { } selected && IsValidAlertStation(selected))
                return selected;

            if (owningStation != null && IsValidAlertStation(owningStation.Value))
            {
                component.SelectedAlertStation = owningStation;
                return owningStation;
            }

            foreach (var station in _stationSystem.GetStations())
            {
                if (!IsValidAlertStation(station))
                    continue;

                component.SelectedAlertStation = station;
                return station;
            }

            component.SelectedAlertStation = null;
            return null;
        }

        private List<CommunicationsConsoleAlertStationState> GetAlertStationStates()
        {
            var result = new List<CommunicationsConsoleAlertStationState>();
            foreach (var station in _stationSystem.GetStations())
            {
                if (!IsValidAlertStation(station))
                    continue;

                result.Add(new CommunicationsConsoleAlertStationState(
                    GetNetEntity(station),
                    MetaData(station).EntityName));
            }

            result.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.CurrentCulture));
            return result;
        }

        private bool IsValidAlertStation(EntityUid station)
        {
            return HasComp<StationDataComponent>(station)
                && TryComp<AlertLevelComponent>(station, out var alert)
                && alert.AlertLevels != null;
        }

        /// <summary>
        /// Attempts to select a station for alert-level changes made from the specified console.
        /// </summary>
        public bool TrySelectAlertStation(
            Entity<CommunicationsConsoleComponent> console,
            EntityUid station,
            EntityUid user)
        {
            if (!CanSelectAlertStation(console, station, user))
                return false;

            console.Comp.SelectedAlertStation = station;
            return true;
        }

        /// <summary>
        /// Checks whether the specified console and user may select the station as an alert-level target.
        /// </summary>
        public bool CanSelectAlertStation(
            Entity<CommunicationsConsoleComponent> console,
            EntityUid station,
            EntityUid user)
        {
            return console.Comp.CanSelectAlertStation
                && CanUse(user, console)
                && IsValidAlertStation(station);
        }
        // Sunrise added end

        private static bool CanAnnounce(CommunicationsConsoleComponent comp)
        {
            return comp.AnnouncementCooldownRemaining <= 0f;
        }

        private bool CanUse(EntityUid user, EntityUid console)
        {
            if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent))
            {
                return _accessReaderSystem.IsAllowed(user, console, accessReaderComponent);
            }
            return true;
        }

        /// <summary>
        /// Checks whether this console is configured to control the specified alert level.
        /// </summary>
        public static bool IsAlertLevelAllowed(
            CommunicationsConsoleComponent console,
            string level,
            AlertLevelDetail detail)
        {
            return console.AllowedAlertLevels?.Contains(level) ?? detail.Selectable;
        }

        private bool CanCallOrRecall(CommunicationsConsoleComponent comp)
        {
            // Defer to what the round end system thinks we should be able to do.
            if (_emergency.EmergencyShuttleArrived || !_roundEndSystem.CanCallOrRecall())
                return false;

            // Ensure that we can communicate with the shuttle (either call or recall)
            if (!comp.CanShuttle)
                return false;

            // Calling shuttle checks
            if (_roundEndSystem.ExpectedCountdownEnd is null)
                return true;

            // Recalling shuttle checks
            var recallThreshold = _cfg.GetCVar(CCVars.EmergencyRecallTurningPoint);

            // shouldn't really be happening if we got here
            if (_roundEndSystem.ShuttleTimeLeft is not { } left
                || _roundEndSystem.ExpectedShuttleLength is not { } expected)
                return false;

            return !(left.TotalSeconds / expected.TotalSeconds < recallThreshold);
        }

        private void OnSelectAlertLevelMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleSelectAlertLevelMessage message)
        {
            if (message.Actor is not { Valid: true } mob)
                return;

            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupCursor(Loc.GetString("comms-console-permission-denied"), message.Actor, PopupType.Medium);
                return;
            }

            var stationUid = ResolveAlertStation(uid, comp); // Sunrise-Edit
            if (stationUid == null
                || !TryComp<AlertLevelComponent>(stationUid.Value, out var alert)
                || alert.AlertLevels == null
                || !alert.AlertLevels.Levels.TryGetValue(message.Level, out var detail)
                || detail.IsAdditional
                || !IsAlertLevelAllowed(comp, message.Level, detail)
                || alert.CurrentLevel == message.Level
                || alert.CurrentDelay > 0)
            {
                return;
            }

            // Привилегированная консоль обходит запрет выбора кода, но не общий cooldown ручных изменений.
            if (comp.ForceAlertLevelChanges)
                StartAlertLevelCooldown(alert);

            _alertLevelSystem.SetLevel(
                stationUid.Value,
                message.Level,
                true,
                true,
                comp.ForceAlertLevelChanges,
                component: alert);
        }

        // Sunrise added start - явное включение или выключение дополнительного кода
        private void OnSetAdditionalAlertLevelMessage(
            EntityUid uid,
            CommunicationsConsoleComponent comp,
            CommunicationsConsoleSetAdditionalAlertLevelMessage message)
        {
            if (message.Actor is not { Valid: true } mob)
                return;

            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupCursor(Loc.GetString("comms-console-permission-denied"), message.Actor, PopupType.Medium);
                return;
            }

            var stationUid = ResolveAlertStation(uid, comp); // Sunrise-Edit
            if (stationUid == null)
                return;

            if (!TryComp<AlertLevelComponent>(stationUid.Value, out var alert)
                || alert.AlertLevels == null
                || !alert.AlertLevels.Levels.TryGetValue(message.Level, out var detail)
                || !detail.IsAdditional
                || !IsAlertLevelAllowed(comp, message.Level, detail))
            {
                return;
            }

            var canChange = comp.ForceAlertLevelChanges
                ? alert.CurrentDelay <= 0 && _alertLevelSystem.CanSetAdditionalLevel(
                    (stationUid.Value, alert),
                    message.Level,
                    message.Enabled,
                    force: true)
                : true;

            if (!canChange)
            {
                UpdateCommsConsoleInterface(uid, comp);
                return;
            }

            if (comp.ForceAlertLevelChanges)
                StartAlertLevelCooldown(alert);

            if (!_alertLevelSystem.TrySetAdditionalLevel(
                stationUid.Value,
                message.Level,
                message.Enabled,
                playSound: true,
                announce: true,
                force: comp.ForceAlertLevelChanges,
                component: alert))
            {
                UpdateCommsConsoleInterface(uid, comp);
            }
        }

        private void StartAlertLevelCooldown(AlertLevelComponent alert)
        {
            alert.CurrentDelay = _cfg.GetCVar(CCVars.GameAlertLevelChangeDelay);
            alert.ActiveDelay = true;
        }

        private void OnSelectAlertStationMessage(
            EntityUid uid,
            CommunicationsConsoleComponent component,
            CommunicationsConsoleSelectAlertStationMessage message)
        {
            if (message.Actor is not { Valid: true } user
                || !TryGetEntity(message.Station, out var stationUid)
                || stationUid is not { } station
                || !TrySelectAlertStation((uid, component), station, user))
                return;

            UpdateCommsConsoleInterface(uid, component);
        }
        // Sunrise added end

        private void OnAnnounceMessage(EntityUid uid, CommunicationsConsoleComponent comp,
            CommunicationsConsoleAnnounceMessage message)
        {
            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(message.Message, maxLength);
            var author = Loc.GetString("comms-console-announcement-unknown-sender");
            if (message.Actor is { Valid: true } mob)
            {
                if (!CanAnnounce(comp))
                {
                    return;
                }

                if (!CanUse(mob, uid))
                {
                    _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                    return;
                }

                var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(uid, mob);
                RaiseLocalEvent(tryGetIdentityShortInfoEvent);
                author = tryGetIdentityShortInfoEvent.Title;
            }

            comp.AnnouncementCooldownRemaining = comp.Delay;
            UpdateCommsConsoleInterface(uid, comp);

            var ev = new CommunicationConsoleAnnouncementEvent(uid, comp, msg, message.Actor);
            RaiseLocalEvent(ref ev);

            // allow admemes with vv
            Loc.TryGetString(comp.Title, out var title);
            title ??= comp.Title;

            if (comp.AnnounceSentBy)
                msg += "\n" + Loc.GetString("comms-console-announcement-sent-by") + " " + author;
            // Sunrise-start
            var voice = comp.AnnounceVoice;
            if (TryComp<TTSComponent>(message.Actor, out var ttsComponent))
            {
                voice = ttsComponent.VoicePrototypeId;
            }
            // Sunrise-end

            if (comp.Global)
            {
                _chatSystem.DispatchGlobalAnnouncement(msg, title, announcementSound: comp.Sound, colorOverride: comp.Color, announceVoice: voice); // Sunrise-edit

                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(message.Actor):player} has sent the following global announcement: {msg}");
                return;
            }

            _chatSystem.DispatchStationAnnouncement(uid, msg, title, colorOverride: comp.Color, announceVoice: voice);

            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(message.Actor):player} has sent the following station announcement: {msg}");

        }

        private void OnBroadcastMessage(EntityUid uid, CommunicationsConsoleComponent component, CommunicationsConsoleBroadcastMessage message)
        {
            if (!TryComp<DeviceNetworkComponent>(uid, out var net))
                return;

            var payload = new NetworkPayload
            {
                [ScreenMasks.Text] = message.Message
            };

            _deviceNetworkSystem.QueuePacket(uid, null, payload, net.TransmitFrequency);

            _adminLogger.Add(LogType.DeviceNetwork, LogImpact.Low, $"{ToPrettyString(message.Actor):player} has sent the following broadcast: {message.Message:msg}");
        }

        private void OnCallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleCallEmergencyShuttleMessage message)
        {
            if (!CanCallOrRecall(comp))
                return;

            var mob = message.Actor;

            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }

            var ev = new CommunicationConsoleCallShuttleAttemptEvent(uid, comp, mob);
            RaiseLocalEvent(ref ev);
            if (ev.Cancelled)
            {
                _popupSystem.PopupEntity(ev.Reason ?? Loc.GetString("comms-console-shuttle-unavailable"), uid, message.Actor);
                return;
            }

            _roundEndSystem.RequestRoundEnd(mob, uid);
            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(mob):player} has called the shuttle.");
        }

        private void OnRecallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleRecallEmergencyShuttleMessage message)
        {
            if (!CanCallOrRecall(comp))
                return;

            var mob = message.Actor;

            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }

            _roundEndSystem.CancelRoundEndCountdown(mob, uid);
            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(message.Actor):player} has recalled the shuttle.");
        }

        // Sunrise-Start
        private void OnToggleRelayMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleToggleRelayMessage message)
        {
            if (comp.IsRelaying)
            {
                StopRelay(uid, comp, announce: true);
                return;
            }

            if (comp.RelayCooldownRemaining > 0f)
                return;

            if (!this.IsPowered(uid, EntityManager))
                return;

            comp.IsRelaying = true;
            comp.RelayTimeRemaining = comp.RelayDuration;
            UpdateCommsConsoleInterface(uid, comp);
            EnsureComp<ActiveListenerComponent>(uid).Range = comp.RelayRange;

            var startText = Loc.GetString("comms-console-relay-started");
            var title = Loc.GetString(comp.Title);
            _chatSystem.DispatchStationAnnouncement(uid, startText, sender: title, playDefault: true, playTts: true, colorOverride: comp.Color, announceVoice: comp.AnnounceVoice, announcementSound: comp.Sound);
        }

        private void OnEntitySpokeNearbyRelay(EntityUid uid, CommunicationsConsoleComponent comp, ListenEvent ev)
        {
            if (!this.IsPowered(uid, EntityManager))
                return;

            var voice = comp.AnnounceVoice;
            if (TryComp<TTSComponent>(ev.Source, out var ttsComponent))
            {
                voice = ttsComponent.VoicePrototypeId;
            }
            _chatSystem.DispatchStationAnnouncement(uid, ev.Message, sender: Loc.GetString(comp.Title), playDefault: false, playTts: true, colorOverride: comp.Color, announceVoice: voice);
        }

        private void StopRelay(EntityUid uid, CommunicationsConsoleComponent comp, bool announce)
        {
            if (!comp.IsRelaying && comp.RelayCooldownRemaining > 0f)
            {
                UpdateCommsConsoleInterface(uid, comp);
                return;
            }

            comp.IsRelaying = false;
            comp.RelayTimeRemaining = 0f;
            comp.RelayCooldownRemaining = comp.RelayCooldown;
            UpdateCommsConsoleInterface(uid, comp);
            RemCompDeferred<ActiveListenerComponent>(uid);

            if (announce)
            {
                var stopText = Loc.GetString("comms-console-relay-stopped");
                var title = Loc.GetString(comp.Title);
                _chatSystem.DispatchStationAnnouncement(uid, stopText, sender: title, playDefault: true, playTts: true, colorOverride: comp.Color, announceVoice: comp.AnnounceVoice, announcementSound: comp.Sound);
            }
        }
        // Sunrise-End
    }

    /// <summary>
    /// Raised on announcement
    /// </summary>
    [ByRefEvent]
    public record struct CommunicationConsoleAnnouncementEvent(EntityUid Uid, CommunicationsConsoleComponent Component, string Text, EntityUid? Sender)
    {
        public EntityUid Uid = Uid;
        public CommunicationsConsoleComponent Component = Component;
        public EntityUid? Sender = Sender;
        public string Text = Text;
    }

    /// <summary>
    /// Raised on shuttle call attempt. Can be cancelled
    /// </summary>
    [ByRefEvent]
    public record struct CommunicationConsoleCallShuttleAttemptEvent(EntityUid Uid, CommunicationsConsoleComponent Component, EntityUid? Sender)
    {
        public bool Cancelled = false;
        public EntityUid Uid = Uid;
        public CommunicationsConsoleComponent Component = Component;
        public EntityUid? Sender = Sender;
        public string? Reason;
    }
}
