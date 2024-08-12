using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Voting.Managers;
using Content.Shared.Voting;

namespace Content.Server._Sunrise.RoundEndVote;

public sealed class RoundEndVoteSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEndSystemChange);
    }

    private void OnRoundEndSystemChange(RoundEndSystemChangedEvent args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
            return;

        // Очень жаль
        //_voteManager.CreateStandardVote(null, StandardVoteType.Preset);
        _voteManager.CreateStandardVote(null, StandardVoteType.Map);
        _gameTicker.SetGamePreset("Secret");
    }
}
