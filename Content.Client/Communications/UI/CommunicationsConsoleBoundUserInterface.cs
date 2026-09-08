using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Communications;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.Communications.UI
{
    public sealed class CommunicationsConsoleBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        [Dependency] private readonly IPlayerManager _player = default!; // Sunrise-Edit

        private readonly AccessReaderSystem _accessReader; // Sunrise-Edit

        [ViewVariables]
        private CommunicationsConsoleMenu? _menu;

        public CommunicationsConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _accessReader = EntMan.System<AccessReaderSystem>(); // Sunrise-Edit
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<CommunicationsConsoleMenu>();
            _menu.OnAnnounce += AnnounceButtonPressed;
            _menu.OnBroadcast += BroadcastButtonPressed;
            _menu.OnAlertLevel += AlertLevelSelected;
            _menu.OnAdditionalAlertLevel += AdditionalAlertLevelSelected; // Sunrise-Edit
            _menu.OnEmergencyLevel += EmergencyShuttleButtonPressed;
            _menu.OnToggleRelay += ToggleRelayPressed; // Sunrise-Edit
        }

        // Sunrise added start - дополнительные коды переключаются независимо
        private void AdditionalAlertLevelSelected(string level, bool enabled)
        {
            if (!HasAccess())
                return;

            SendMessage(new CommunicationsConsoleSetAdditionalAlertLevelMessage(level, enabled));
        }
        // Sunrise added end

        public void AlertLevelSelected(string level)
        {
            if (_menu!.AlertLevelSelectable && HasAccess()) // Sunrise-Edit
            {
                _menu.CurrentLevel = level;
                SendMessage(new CommunicationsConsoleSelectAlertLevelMessage(level));
            }
        }

        public void EmergencyShuttleButtonPressed()
        {
            if (_menu!.CountdownStarted)
                RecallShuttle();
            else
                CallShuttle();
        }

        public void AnnounceButtonPressed(string message)
        {
            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(message, maxLength);
            SendMessage(new CommunicationsConsoleAnnounceMessage(msg));
        }

        public void BroadcastButtonPressed(string message)
        {
            SendMessage(new CommunicationsConsoleBroadcastMessage(message));
        }

        // Sunrise-Start
        private void ToggleRelayPressed()
        {
            SendMessage(new CommunicationsConsoleToggleRelayMessage());
        }
        // Sunrise-End

        public void CallShuttle()
        {
            SendMessage(new CommunicationsConsoleCallEmergencyShuttleMessage());
        }

        public void RecallShuttle()
        {
            SendMessage(new CommunicationsConsoleRecallEmergencyShuttleMessage());
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not CommunicationsConsoleInterfaceState commsState)
                return;

            if (_menu != null)
            {
                var hasAccess = HasAccess(); // Sunrise-Edit
                _menu.CanAnnounce = commsState.CanAnnounce;
                _menu.CanBroadcast = commsState.CanBroadcast;
                _menu.CanCall = commsState.CanCall;
                _menu.CountdownStarted = commsState.CountdownStarted;
                _menu.AlertLevelSelectable = hasAccess
                    && commsState.AlertLevels != null
                    && !float.IsNaN(commsState.CurrentAlertDelay)
                    && commsState.CurrentAlertDelay <= 0; // Sunrise-Edit
                _menu.CurrentLevel = commsState.CurrentAlert;
                _menu.CountdownEnd = commsState.ExpectedCountdownEnd;

                _menu.UpdateCountdown();
                _menu.UpdateAlertLevels(commsState.AlertLevels, _menu.CurrentLevel);
                _menu.UpdateAdditionalAlertLevels(commsState.AdditionalAlertLevels, hasAccess); // Sunrise-Edit
                _menu.AlertLevelButton.Disabled = !_menu.AlertLevelSelectable;
                _menu.EmergencyShuttleButton.Disabled = !_menu.CanCall;
                _menu.AnnounceButton.Disabled = !_menu.CanAnnounce;
                _menu.BroadcastButton.Disabled = !_menu.CanBroadcast;

                // Sunrise-Start
                _menu.CanRelay = commsState.CanRelay;
                _menu.IsRelaying = commsState.IsRelaying;
                _menu.RelayCooldownRemaining = commsState.RelayCooldownRemaining;
                _menu.RelayTimeRemaining = commsState.RelayTimeRemaining;
                _menu.UpdateRelayUi();
                // Sunrise-End
            }
        }

        // Sunrise added start - сразу блокируем управление кодами без требуемого доступа
        private bool HasAccess()
        {
            return _player.LocalEntity is { } player && _accessReader.IsAllowed(player, Owner);
        }
        // Sunrise added end
    }
}
