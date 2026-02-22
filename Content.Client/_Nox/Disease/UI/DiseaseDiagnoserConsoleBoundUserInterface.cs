// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Content.Shared._Nox.Disease;
using System.Linq;

namespace Content.Client._Nox.Disease.UI
{
    [UsedImplicitly]
    public sealed class DiseaseDiagnoserConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private DiseaseDiagnoserConsoleWindow? _window;

        public DiseaseDiagnoserConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        { }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<DiseaseDiagnoserConsoleWindow>();

            _window.ScanDiseaseButton.OnPressed += _ =>
                SendMessage(new UiButtonPressedMessage(UiButton.ScanDisease, null));

            _window.StartAnalysButton.OnPressed += _ =>
                SendMessage(new UiButtonPressedMessage(UiButton.StartAnalys, null));

            _window.GenerateDiseaseButton.OnPressed += _ =>
            {
                var strainName = GenSelectedRecord();
                SendMessage(new UiButtonPressedMessage(UiButton.GenerateDisease, strainName));
            };

            _window.PrintReportButton.OnPressed += _ =>
            {
                var strainName = GenSelectedRecord();
                SendMessage(new UiButtonPressedMessage(UiButton.PrintReport, strainName));
            };

            _window.DeleteStrainButton.OnPressed += _ =>
            {
                var strainName = GenSelectedRecord();
                SendMessage(new UiButtonPressedMessage(UiButton.DeleteData, strainName));
            };
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            _window?.Populate((DiseaseDiagnoserConsoleBoundUserInterfaceState)state);
        }

        private string? GenSelectedRecord()
        {
            if (_window == null)
                return null;

            var strainName = _window.StrainList.GetSelected().FirstOrDefault()?.Text;

            if (strainName == null)
                return null;

            return strainName.Split('-')[0];
        }
    }
}
