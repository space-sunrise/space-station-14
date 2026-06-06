using Content.Server.GameTicking;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;

namespace Content.Server._Sunrise.Storyteller.Systems;

/// <summary>
/// Handles persistent cooldown state for storyteller presets.
/// Puts the Insane storyteller on cooldown if it was just played,
/// and removes the cooldown if a different preset was played.
/// </summary>
public sealed class StorytellerRotationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    public static readonly string StorytellerInsaneId = "StorytellerInsane";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (!_cfg.GetCVar(SunriseCCVars.StorytellerRotationEnabled))
            return;

        if (ev.New != GameRunLevel.PreRoundLobby)
            return;

        if (ev.Old == GameRunLevel.InRound || ev.Old == GameRunLevel.PostRound)
        {
            var currentPreset = _ticker.CurrentPreset;
            if (currentPreset == null)
                return;

            var nextState = currentPreset.ID == StorytellerInsaneId ? 1 : 0;

            _cfg.SetCVar(SunriseCCVars.StorytellerRotationCounter, nextState);

            var sawmill = Logger.GetSawmill("storyteller");
            sawmill.Info($"Storyteller cooldown state updated to {nextState} after playing {currentPreset.ID}");
        }
    }
}
