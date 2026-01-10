using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared._Sunrise.Ghost;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.Ghost
{
    public sealed class AcceptGhostEui : BaseEui
    {
        private readonly EntityUid _mindId;
        private readonly GhostSystem _ghostSystem;

        public AcceptGhostEui(EntityUid mindId, GhostSystem ghostSystem)
        {
            _mindId = mindId;
            _ghostSystem = ghostSystem;
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            if (msg is not AcceptGhostChoiceMessage choice)
            {
                Close();
                return;
            }
            if (Player.AttachedEntity is { Valid: true } entity)
            {
                if (choice.Button == AcceptGhostUiButton.Accept)
                {
                    _ghostSystem.TrySendPendingLastWords(entity);
                }
                else
                {
                    _ghostSystem.CancelPendingLastWords(entity);
                }
            }

            if (choice.Button == AcceptGhostUiButton.Accept)
                _ghostSystem.OnGhostAttempt(_mindId, canReturnGlobal: true);
            Close();
        }
    }
}
