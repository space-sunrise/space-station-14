using System.Linq;
using Content.Client.Eui;
using Content.Client.Lobby;
using Content.Shared._Sunrise.NewLife;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Client._Sunrise.NewLife;

[UsedImplicitly]
public sealed class NewLifeEui : BaseEui
{
    private readonly NewLifeWindow _window;
    private readonly IGameTiming _timing;
    private NewLifeEuiState? _state;

    public NewLifeEui()
    {
        _timing = IoCManager.Resolve<IGameTiming>();
        _window = new NewLifeWindow(_timing);
        var preferencesManager = IoCManager.Resolve<IClientPreferencesManager>();

        _window.SpawnRequested += () =>
        {
            if (_state == null)
                return;

            var validation = NewLifeRequestValidation.Validate(
                _state,
                _timing.CurTime,
                _window.GetSelectedCharacter(),
                _window.GetSelectedStation(),
                _window.GetSelectedRole());

            if (validation != NewLifeRequestValidationResult.Valid)
                return;

            var selectedCharacter = _window.GetSelectedCharacter();

            if (selectedCharacter != null)
                preferencesManager.SelectCharacter(selectedCharacter.Value);
            SendMessage(new NewLifeRequestSpawnMessage(_window.GetSelectedCharacter(), _window.GetSelectedStation(), _window.GetSelectedRole()));
        };

        _window.OnClose += () =>
        {
            SendMessage(new CloseEuiMessage());
        };
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not NewLifeEuiState newLifeState)
            return;

        _state = newLifeState;

        if (newLifeState.Stations.Count == 0 || newLifeState.Jobs.Count == 0)
        {
            _window.UpdateValidationState(newLifeState);
            _window.UpdateCharactersList(newLifeState.Characters, newLifeState.UsedCharactersForRespawn);
            _window.UpdateStationList(new Dictionary<NetEntity, string>(), default);
            _window.UpdateRolesList(new List<NewLifeRolesInfo>());
            _window.UpdateJobs(newLifeState.Jobs);
            _window.UpdateNextRespawn(newLifeState.NextRespawnTime);
            return;
        }

        var selectedStation = newLifeState.Stations.Keys.First();

        _window.UpdateValidationState(newLifeState);
        _window.UpdateCharactersList(newLifeState.Characters, newLifeState.UsedCharactersForRespawn);
        _window.UpdateStationList(newLifeState.Stations, selectedStation);
        _window.UpdateRolesList(newLifeState.Jobs[selectedStation]);
        _window.UpdateJobs(newLifeState.Jobs);
        _window.UpdateNextRespawn(newLifeState.NextRespawnTime);
    }
}

