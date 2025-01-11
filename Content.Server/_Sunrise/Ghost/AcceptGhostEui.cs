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

            if (msg is not AcceptGhostChoiceMessage choice ||
                choice.Button == AcceptGhostUiButton.Deny)
            {
                Close();
                return;
            }

            _ghostSystem.OnGhostAttempt(_mindId, canReturnGlobal: true);
            Close();
        }
    }
}
