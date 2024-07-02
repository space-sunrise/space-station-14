using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Voting.Managers;
using Content.Shared.Voting;
using System.Threading.Tasks;

namespace Content.Server._Sunrise.RoundEndVote;

public sealed class RoundEndVoteSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;
	
	private bool _isWaitingForVote = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndSystemChangedEvent>(OnRoundEndSystemChange);
    }

    private async void OnRoundEndSystemChange(RoundEndSystemChangedEvent args)
    {
        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
            return;
		
		if (_isWaitingForVote)
            return;

        _isWaitingForVote = true;
		
		await Task.Delay(TimeSpan.FromSeconds(120));

        // Очень жаль
        //_voteManager.CreateStandardVote(null, StandardVoteType.Preset);
        _voteManager.CreateStandardVote(null, StandardVoteType.Map);
        _gameTicker.SetGamePreset("Secret");
		
		_isWaitingForVote = false;
    }
}
