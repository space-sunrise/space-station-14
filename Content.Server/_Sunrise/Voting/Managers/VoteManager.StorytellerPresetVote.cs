using System.Linq;
using Content.Server._Sunrise.Presets;
using Content.Server._Sunrise.Storyteller.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Voting.Managers;

public sealed partial class VoteManager
{
    [Dependency] private readonly ILocalizationManager _loc = default!;

    private const string StorytellerVoteOptionId = "__storyteller__";

    /// <summary>
    /// Gets eligible non-Storyteller presets for the first stage of the Sunrise preset vote.
    /// </summary>
    /// <param name="excludedPresets">
    /// Preset IDs to omit. Each set item is a <see cref="GamePresetPrototype.ID"/> temporarily excluded by rotation;
    /// <see langword="null"/> leaves all configured regular presets eligible for consideration.
    /// </param>
    /// <returns>
    /// A dictionary whose key is a regular <see cref="GamePresetPrototype.ID"/> and whose value is its
    /// <see cref="GamePresetPrototype.ModeTitle"/> localization ID, not localized text.
    /// </returns>
    private Dictionary<string, string> GetSunriseRegularPresetsForVote(IReadOnlySet<string>? excludedPresets = null)
    {
        var ticker = _entityManager.System<GameTicker>();
        var presetPoolId = _cfg.GetCVar(SunriseCCVars.GamePresetPool);

        if (!_prototypeManager.TryIndex<GamePresetPoolPrototype>(presetPoolId, out var presetPoolProto))
            return new Dictionary<string, string>();

        var eligiblePresets = ticker.GetEligibleVotePresets(
            presetPoolProto.Presets,
            _playerManager.PlayerCount,
            excludedPresets);

        var result = new Dictionary<string, string>();

        foreach (var (presetId, title) in eligiblePresets)
        {
            if (!StorytellerSystem.IsStorytellerPreset(presetId))
                result[presetId] = title;
        }

        return result;
    }

    private (Dictionary<string, string> TopLevel, Dictionary<string, string> Storyteller, bool ResetExclusions)
        GetSunrisePresetVoteChoices()
    {
        var ticker = _entityManager.System<GameTicker>();
        var storyteller = _entityManager.System<StorytellerSystem>();

        var excludedPresets = ticker.ExcludedPresets.ToHashSet();
        var regularPresets = GetSunriseRegularPresetsForVote(excludedPresets);
        var storytellerPresets = storyteller.GetAvailableVotePresets(excludedPresets);

        var resetExclusions = false;
        if (regularPresets.Count == 0 && storytellerPresets.Count == 0 && excludedPresets.Count > 0)
        {
            regularPresets = GetSunriseRegularPresetsForVote();
            storytellerPresets = storyteller.GetAvailableVotePresets(new HashSet<string>());
            resetExclusions = true;
        }

        var result = new Dictionary<string, string>(regularPresets);

        if (storytellerPresets.Count > 0)
            result[StorytellerVoteOptionId] = "ui-vote-storyteller-entry";

        return (result, storytellerPresets, resetExclusions);
    }

    private bool CanCallSunrisePresetVote()
    {
        var (presets, storytellerPresets, _) = GetSunrisePresetVoteChoices();

        if (presets.Count == 0)
            return false;

        if (presets.Count > 1)
            return true;

        var ticker = _entityManager.System<GameTicker>();
        var singleTopLevelPreset = presets.Keys.First();

        if (singleTopLevelPreset != StorytellerVoteOptionId)
            return singleTopLevelPreset != ticker.Preset?.ID;

        return storytellerPresets.Count != 1 || storytellerPresets.Keys.First() != ticker.Preset?.ID;
    }

    private bool TryCreateSunriseTwoStagePresetVote(ICommonSession? initiator)
    {
        var (presets, _, resetExclusions) = GetSunrisePresetVoteChoices();

        if (resetExclusions)
            _entityManager.System<GameTicker>().ClearExcludedPresets();

        if (presets.Count == 0)
            return true;

        if (presets.Count == 1)
        {
            var singlePreset = presets.First();

            if (singlePreset.Key == StorytellerVoteOptionId)
            {
                CreateSunriseStorytellerTypeVote(initiator);
                return true;
            }

            _chatManager.DispatchServerAnnouncement(
                _loc.GetString("ui-vote-gamemode-auto-set", ("preset", _loc.GetString(singlePreset.Value))));
            _entityManager.System<GameTicker>().SetGamePreset(singlePreset.Key);
            return true;
        }

        var options = CreateSunrisePresetVoteOptions(_loc.GetString("ui-vote-gamemode-title"), initiator);

        foreach (var (presetId, title) in presets)
        {
            options.Options.Add((_loc.GetString(title), presetId));
        }

        var vote = CreateVote(options);

        vote.OnFinished += (_, args) =>
        {
            string picked;
            if (args.Winner == null)
            {
                picked = (string) _random.Pick(args.Winners);
                _chatManager.DispatchServerAnnouncement(_loc.GetString("ui-vote-gamemode-tie"));
            }
            else
            {
                picked = (string) args.Winner;
                _chatManager.DispatchServerAnnouncement(_loc.GetString("ui-vote-gamemode-win"));
            }

            var loggedPreset = picked == StorytellerVoteOptionId ? "Storyteller" : picked;
            _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Preset vote finished: {loggedPreset}");

            if (picked == StorytellerVoteOptionId)
            {
                CreateSunriseStorytellerTypeVote(initiator);
                return;
            }

            _entityManager.System<GameTicker>().SetGamePreset(picked);
        };

        return true;
    }

    private void CreateSunriseStorytellerTypeVote(ICommonSession? initiator)
    {
        var storytellerPresets = _entityManager.System<StorytellerSystem>().GetAvailableVotePresets();

        if (storytellerPresets.Count == 0)
            return;

        if (storytellerPresets.Count == 1)
        {
            var singleSubtypeId = storytellerPresets.Keys.First();
            var singleSubtypeName = _loc.GetString(StorytellerSystem.GetNameLocId(singleSubtypeId));

            _chatManager.DispatchServerAnnouncement(
                _loc.GetString("ui-vote-storyteller-auto-set", ("type", singleSubtypeName)));
            _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Storyteller type vote skipped, auto-selected: {singleSubtypeId}");
            _entityManager.System<GameTicker>().SetGamePreset(singleSubtypeId);
            return;
        }

        var options = CreateSunrisePresetVoteOptions(_loc.GetString("ui-vote-storyteller-title"), initiator);

        foreach (var presetId in storytellerPresets.Keys)
        {
            options.Options.Add((_loc.GetString(StorytellerSystem.GetVoteOptionLocId(presetId)), presetId));
        }

        var vote = CreateVote(options);

        vote.OnFinished += (_, args) =>
        {
            string picked;
            if (args.Winner == null)
            {
                picked = (string) _random.Pick(args.Winners);
                _chatManager.DispatchServerAnnouncement(
                    _loc.GetString("ui-vote-storyteller-type-tie",
                        ("type", _loc.GetString(StorytellerSystem.GetNameLocId(picked)))));
            }
            else
            {
                picked = (string) args.Winner;
                _chatManager.DispatchServerAnnouncement(
                    _loc.GetString("ui-vote-storyteller-type-win",
                        ("type", _loc.GetString(StorytellerSystem.GetNameLocId(picked)))));
            }

            _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Storyteller type vote finished: {picked}");
            _entityManager.System<GameTicker>().SetGamePreset(picked);
        };
    }

    private VoteOptions CreateSunrisePresetVoteOptions(string title, ICommonSession? initiator)
    {
        var alone = _playerManager.PlayerCount == 1 && initiator != null;
        var options = new VoteOptions
        {
            Title = title,
            Duration = alone
                ? TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteTimerAlone))
                : TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.VoteTimerPreset)),
            DisplayVotes = _cfg.GetCVar(SunriseCCVars.ShowPresetVotes),
        };

        if (alone)
            options.InitiatorTimeout = TimeSpan.FromSeconds(10);

        WirePresetVoteInitiator(options, initiator);
        return options;
    }
}
