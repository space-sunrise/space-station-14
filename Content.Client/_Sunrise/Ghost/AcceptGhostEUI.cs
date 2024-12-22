using Content.Client.Eui;
using Content.Shared._Sunrise.Ghost;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._Sunrise.Ghost
{
    [UsedImplicitly]
    public sealed class AcceptGhostEui : BaseEui
    {
        private readonly AcceptGhostWindow _window;

        public AcceptGhostEui()
        {
            _window = new AcceptGhostWindow();

            _window.DenyButton.OnPressed += _ =>
            {
                SendMessage(new AcceptGhostChoiceMessage(AcceptGhostUiButton.Deny));
                _window.Close();
            };

            _window.AcceptButton.OnPressed += _ =>
            {
                SendMessage(new AcceptGhostChoiceMessage(AcceptGhostUiButton.Accept));
                _window.Close();
            };
        }

        public override void Opened()
        {
            IoCManager.Resolve<IClyde>().RequestWindowAttention();
            _window.OpenCentered();
        }

        public override void Closed()
        {
            _window.Close();
        }

    }
}
