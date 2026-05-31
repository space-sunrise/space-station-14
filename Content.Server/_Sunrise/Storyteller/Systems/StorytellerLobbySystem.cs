using Content.Server.GameTicking;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Server._Sunrise.Storyteller.Components;
using Content.Server.Chat.Managers;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server._Sunrise.Storyteller.Systems;

/// <summary>
/// Dedicated system managing lobby voting and countdown pausing for the Storyteller preset type selection.
/// </summary>
public sealed class StorytellerLobbySystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public StorytellerType StorytellerType = StorytellerType.Classic;
    private bool _storytellerTypeVoteStarted = false;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.PreRoundLobby)
        {
            _storytellerTypeVoteStarted = false;
            StorytellerType = StorytellerType.Classic;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTicker.RunLevel != GameRunLevel.PreRoundLobby || _gameTicker.Paused || _storytellerTypeVoteStarted)
            return;

        if (_gameTicker.Preset?.ID != StorytellerPresetHelper.StorytellerPresetId)
            return;

        if (_gameTicker.RoundStartTime == TimeSpan.Zero)
            return;

        // Trigger storyteller type vote 30 seconds before round start
        if (_gameTicker.RoundStartTime - TimeSpan.FromSeconds(30) <= _gameTiming.CurTime)
        {
            _storytellerTypeVoteStarted = true;
            StartStorytellerTypeVote();
        }
    }

    private void StartStorytellerTypeVote()
    {
        var options = new VoteOptions
        {
            Title = Loc.GetString("ui-vote-storyteller-type-title"),
            InitiatorText = Loc.GetString("ui-vote-initiator-server"), // Sunrise-Edit
            Options =
            {
                (Loc.GetString("ui-vote-storyteller-type-calm"), StorytellerType.Calm),
                (Loc.GetString("ui-vote-storyteller-type-classic"), StorytellerType.Classic),
                (Loc.GetString("ui-vote-storyteller-type-insane"), StorytellerType.Insane)
            },
            Duration = TimeSpan.FromSeconds(15),
            DisplayVotes = true
        };

        var vote = _voteManager.CreateVote(options);

        vote.OnFinished += (_, args) =>
        {
            StorytellerType picked;
            if (args.Winner == null)
            {
                picked = (StorytellerType) _random.Pick(args.Winners);
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("ui-vote-storyteller-type-tie", ("type", Loc.GetString($"ui-vote-storyteller-type-{picked.ToString().ToLower()}-name"))));
            }
            else
            {
                picked = (StorytellerType) args.Winner;
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("ui-vote-storyteller-type-win", ("type", Loc.GetString($"ui-vote-storyteller-type-{picked.ToString().ToLower()}-name"))));
            }

            StorytellerType = picked;
        };
    }
}
