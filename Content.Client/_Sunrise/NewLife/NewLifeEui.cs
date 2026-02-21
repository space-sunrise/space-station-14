using System.Linq;
using Content.Client.Eui;
using Content.Client.Lobby;
using Content.Client._Sunrise.PlanetPrison;
using Content.Shared._Sunrise.NewLife;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;


namespace Content.Client._Sunrise.NewLife
{
    [UsedImplicitly]
    public sealed class NewLifeEui : BaseEui
    {
        private readonly NewLifeWindow _window;
        private PlanetPrisonUISystem? _prisonUi;

        public NewLifeEui()
        {
            _window = new NewLifeWindow(IoCManager.Resolve<IGameTiming>());
            var preferencesManager = IoCManager.Resolve<IClientPreferencesManager>();

            _window.SpawnRequested += () =>
            {
                var selectedCharacter = _window.GetSelectedCharacter();

                if (selectedCharacter != null)
                    preferencesManager.SelectCharacter(selectedCharacter.Value);
                SendMessage(new NewLifeRequestSpawnMessage(_window.GetSelectedCharacter(), _window.GetSelectedStation(), _window.GetSelectedRole()));
            };

            _window.OnClose += () =>
            {
                SendMessage(new CloseEuiMessage());
            };

            var prisonUi = EntitySystem.Get<PlanetPrisonUISystem>();
            _prisonUi = prisonUi;
            prisonUi.PrisonTimerActiveChanged += OnPrisonTimerActiveChanged;
        }

        public override void Opened()
        {
            base.Opened();
            _window.OpenCentered();
        }

        public override void Closed()
        {
            base.Closed();
            if (_prisonUi != null)
            {
                _prisonUi.PrisonTimerActiveChanged -= OnPrisonTimerActiveChanged;
                _prisonUi = null;
            }
            _window.Close();
        }

        public override void HandleState(EuiStateBase state)
        {
            base.HandleState(state);

            if (state is not NewLifeEuiState newLifeState)
                return;

            var selectedStation = newLifeState.Stations.Keys.FirstOrDefault();

            _window.UpdateCharactersList(newLifeState.Characters, newLifeState.UsedCharactersForRespawn);
            _window.UpdateStationList(newLifeState.Stations, selectedStation);
            _window.UpdateRolesList(newLifeState.Jobs[selectedStation]);
            _window.UpdateJobs(newLifeState.Jobs);
            _window.UpdateNextRespawn(newLifeState.NextRespawnTime);
        }

        private void OnPrisonTimerActiveChanged(bool active)
        {
            _window.SetPrisonTimerActive(active);

            if (active && _window.IsOpen)
            {
                _window.Close();
            }
        }
    }
}
