using Content.Shared._Sunrise.Pacificator;
using JetBrains.Annotations;

namespace Content.Client._Sunrise.Pacificator
{
    [UsedImplicitly]
    public sealed class PacificatorBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private PacificatorWindow? _window;

        public PacificatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new PacificatorWindow(this);

            /*
            _window.Switch.OnPressed += _ =>
            {
                SendMessage(new SharedPacificatorComponent.SwitchGeneratorMessage(!IsOn));
            };
            */

            _window.OpenCentered();
            _window.OnClose += Close;
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (GeneratorState) state;
            _window?.UpdateState(castState);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            _window?.Dispose();
        }

        public void SetPowerSwitch(bool on)
        {
            SendMessage(new SwitchGeneratorMessage(on));
        }
    }
}
