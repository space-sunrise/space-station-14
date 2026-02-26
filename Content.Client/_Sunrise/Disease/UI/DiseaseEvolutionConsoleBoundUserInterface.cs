// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using System.Linq;
using Content.Shared.Body.Prototypes;
using Content.Shared._Sunrise.Disease.Prototypes;
using Content.Shared._Sunrise.Disease;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Disease.UI
{
    [UsedImplicitly]
    public sealed class DiseaseEvolutionConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private DiseaseEvolutionConsoleWindow? _window;

        public DiseaseEvolutionConsoleBoundUserInterface(EntityUid owner, Enum uiKey)
            : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<DiseaseEvolutionConsoleWindow>();

            // Покупка симптома
            _window.BuySymptomButton.OnPressed += _ =>
                SendMessage(new EvolutionConsoleUiButtonPressedMessage(
                    EvolutionConsoleUiButton.EvolutionSymptom,
                    symptom: GenSelectedAvailableSymptom()
                ));

            // Покупка тела
            _window.BuyBodyButton.OnPressed += _ =>
                SendMessage(new EvolutionConsoleUiButtonPressedMessage(
                    EvolutionConsoleUiButton.EvolutionBody,
                    body: GenSelectedAvailableBody()
                ));

            // Удаление симптома
            _window.DeleteSymptomButton.OnPressed += _ =>
                SendMessage(new EvolutionConsoleUiButtonPressedMessage(
                    EvolutionConsoleUiButton.DeleteSymptom,
                    symptom: GenSelectedActiveSymptom()
                ));

            // Удаление тела
            _window.DeleteBodyButton.OnPressed += _ =>
                SendMessage(new EvolutionConsoleUiButtonPressedMessage(
                    EvolutionConsoleUiButton.DeleteBody,
                    body: GenSelectedActiveBody()
                ));
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            _window?.Populate((DiseaseEvolutionConsoleBoundUserInterfaceState)state);
        }

        private string? GenSelectedAvailableSymptom()
        {
            if (_window == null)
                return null;

            var item = _window.AvailableSymptomsList.GetSelected().FirstOrDefault();
            return item?.Metadata as string;
        }

        private string? GenSelectedAvailableBody()
        {
            if (_window == null)
                return null;

            var item = _window.AvailableBodiesList.GetSelected().FirstOrDefault();
            return item?.Metadata as string;
        }

        private string? GenSelectedActiveSymptom()
        {
            if (_window == null)
                return null;

            var item = _window.ActiveSymptomsList.GetSelected().FirstOrDefault();
            if (item?.Metadata is ProtoId<DiseaseSymptomPrototype> id)
                return id.Id;

            return null;
        }

        private string? GenSelectedActiveBody()
        {
            if (_window == null)
                return null;

            var item = _window.ActiveBodiesList.GetSelected().FirstOrDefault();
            if (item?.Metadata is ProtoId<BodyPrototype> id)
                return id.Id;

            return null;
        }
    }
}
