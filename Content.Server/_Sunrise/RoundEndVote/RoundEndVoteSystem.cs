using System.Threading.Tasks;
using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Voting.Managers;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Voting;
using Content.Shared.GameTicking;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;

namespace Content.Server._Sunrise.RoundEndVote;

public sealed class RoundEndVoteSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedGameTicker _sharedgameTicker = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEndSystemChange);
    }
    
    public async void OnRoundEndSystemChange(RoundEndSystemChangedEvent args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
            return;
        
        var delay = Math.Abs((_gameTicker.LobbyDuration - TimeSpan.FromSeconds(75)).TotalMilliseconds);
        
        Log.Warning($"waiting {(int)delay}");
        await Task.Delay((int)delay);
        StartRoundEndVotes();
    }
    
    public void StartRoundEndVotes()
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
            return;

        if (_cfg.GetCVar(SunriseCCVars.RunMapVoteAfterRestart))
            _voteManager.CreateStandardVote(null, StandardVoteType.Map);

        if (_cfg.GetCVar(SunriseCCVars.RunPresetVoteAfterRestart))
            _voteManager.CreateStandardVote(null, StandardVoteType.Preset);

        if (_cfg.GetCVar(SunriseCCVars.ResetPresetAfterRestart))
            _gameTicker.SetGamePreset("Secret");
    }
}
