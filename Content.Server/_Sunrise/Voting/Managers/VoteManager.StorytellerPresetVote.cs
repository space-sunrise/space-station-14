using System;
using System.Linq;
using Content.Server._Sunrise.Presets;
using Content.Server._Sunrise.Storyteller;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Voting;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Voting.Managers;

public sealed partial class VoteManager
{
    private const string StorytellerVoteOptionId = "__storyteller__";

    private Dictionary<string, string> GetSunriseStorytellerPresetsForVote()
    {
        var playerCount = _playerManager.PlayerCount;

        var result = new Dictionary<string, string>();
        var storytellerPool = new Dictionary<string, int[]>();

        StorytellerPresetHelper.AdjustPresetPool(storytellerPool, _cfg, playerCount);

        foreach (var (presetId, limits) in storytellerPool)
        {
            if (!_prototypeManager.TryIndex<GamePresetPrototype>(presetId, out var preset))
                continue;

            if (!preset.ShowInVote)
                continue;

            var minPlayers = limits.Length > 0 ? limits[0] : int.MinValue;
            var maxPlayers = limits.Length > 1 ? limits[1] : int.MaxValue;

            if (playerCount < minPlayers || playerCount > maxPlayers)
                continue;

            result[preset.ID] = preset.ModeTitle;
        }

        return result;
    }

    private Dictionary<string, string> GetSunriseRegularPresetsForVote()
    {
        var ticker = _entityManager.System<GameTicker>();
        var presetPoolId = _cfg.GetCVar(SunriseCCVars.GamePresetPool);

        if (!_prototypeManager.TryIndex<GamePresetPoolPrototype>(presetPoolId, out var presetPoolProto))
            return new Dictionary<string, string>();

        Dictionary<string, string> BuildRegularPresetList(HashSet<string>? excludedPresets = null)
        {
            var playerCount = _playerManager.PlayerCount;
            var result = new Dictionary<string, string>();

            foreach (var (presetId, limits) in presetPoolProto.Presets)
            {
                if (StorytellerPresetHelper.ShouldBypassExclusion(presetId))
                    continue;

                if (excludedPresets != null && excludedPresets.Contains(presetId))
                    continue;

                if (!_prototypeManager.TryIndex<GamePresetPrototype>(presetId, out var preset))
                    continue;

                if (!preset.ShowInVote)
                    continue;

                var minPlayers = limits.Length > 0 ? limits[0] : int.MinValue;
                var maxPlayers = limits.Length > 1 ? limits[1] : int.MaxValue;

                if (playerCount < minPlayers || playerCount > maxPlayers)
                    continue;

                result[preset.ID] = preset.ModeTitle;
            }

            return result;
        }

        var excluded = ticker.ExcludedPresets.ToHashSet();
        var regularPresets = BuildRegularPresetList(excluded);

        if (regularPresets.Count == 0 && excluded.Count > 0)
        {
            ticker.ClearExcludedPresets();
            regularPresets = BuildRegularPresetList();
        }

        return regularPresets;
    }

    private Dictionary<string, string> GetSunriseTopLevelPresetsForVote()
    {
        var regularPresets = GetSunriseRegularPresetsForVote();
        var storytellerPresets = GetSunriseStorytellerPresetsForVote();
        var result = new Dictionary<string, string>(regularPresets);

        if (storytellerPresets.Count > 0)
            result[StorytellerVoteOptionId] = "ui-vote-storyteller-entry";

        return result;
    }

    private bool CanCallSunrisePresetVote()
    {
        var presets = GetSunriseTopLevelPresetsForVote();

        if (presets.Count == 0)
            return false;

        if (presets.Count > 1)
            return true;

        var ticker = _entityManager.System<GameTicker>();
        var singleTopLevelPreset = presets.Keys.First();

        if (singleTopLevelPreset != StorytellerVoteOptionId)
            return singleTopLevelPreset != ticker.Preset?.ID;

        var storytellerPresets = GetSunriseStorytellerPresetsForVote();

        if (storytellerPresets.Count == 0)
            return false;

        if (storytellerPresets.Count == 1 && storytellerPresets.Keys.First() == ticker.Preset?.ID)
            return false;

        return true;
    }

    private bool TryCreateSunriseTwoStagePresetVote(ICommonSession? initiator)
    {
        var presets = GetSunriseTopLevelPresetsForVote();

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
                Loc.GetString("ui-vote-gamemode-auto-set", ("preset", Loc.GetString(singlePreset.Value))));
            _entityManager.System<GameTicker>().SetGamePreset(singlePreset.Key);
            return true;
        }

        var options = CreateSunrisePresetVoteOptions(Loc.GetString("ui-vote-gamemode-title"), initiator);

        foreach (var (presetId, title) in presets)
        {
            options.Options.Add((Loc.GetString(title), presetId));
        }

        var vote = CreateVote(options);

        vote.OnFinished += (_, args) =>
        {
            string picked;
            if (args.Winner == null)
            {
                picked = (string) _random.Pick(args.Winners);
                _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-gamemode-tie"));
            }
            else
            {
                picked = (string) args.Winner;
                _chatManager.DispatchServerAnnouncement(Loc.GetString("ui-vote-gamemode-win"));
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
        var storytellerPresets = GetSunriseStorytellerPresetsForVote();

        if (storytellerPresets.Count == 0)
            return;

        if (storytellerPresets.Count == 1)
        {
            var singleSubtypeId = storytellerPresets.Keys.First();
            var singleSubtypeName = Loc.GetString(GetSunriseStorytellerTypeName(singleSubtypeId));

            _chatManager.DispatchServerAnnouncement(
                Loc.GetString("ui-vote-storyteller-auto-set", ("type", singleSubtypeName)));
            _adminLogger.Add(LogType.Vote, LogImpact.Medium, $"Storyteller type vote skipped, auto-selected: {singleSubtypeId}");
            _entityManager.System<GameTicker>().SetGamePreset(singleSubtypeId);
            return;
        }

        var options = CreateSunrisePresetVoteOptions(Loc.GetString("ui-vote-storyteller-title"), initiator);

        foreach (var presetId in storytellerPresets.Keys)
        {
            options.Options.Add((Loc.GetString(GetSunriseStorytellerTypeOption(presetId)), presetId));
        }

        var vote = CreateVote(options);

        vote.OnFinished += (_, args) =>
        {
            string picked;
            if (args.Winner == null)
            {
                picked = (string) _random.Pick(args.Winners);
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("ui-vote-storyteller-type-tie",
                        ("type", Loc.GetString(GetSunriseStorytellerTypeName(picked)))));
            }
            else
            {
                picked = (string) args.Winner;
                _chatManager.DispatchServerAnnouncement(
                    Loc.GetString("ui-vote-storyteller-type-win",
                        ("type", Loc.GetString(GetSunriseStorytellerTypeName(picked)))));
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

    private string GetSunriseStorytellerTypeOption(string presetId)
    {
        return presetId switch
        {
            StorytellerPresetHelper.StorytellerCalmId => "ui-vote-storyteller-type-calm",
            StorytellerPresetHelper.StorytellerClassicId => "ui-vote-storyteller-type-classic",
            StorytellerPresetHelper.StorytellerInsaneId => "ui-vote-storyteller-type-insane",
            _ => GetSunriseStorytellerTypeName(presetId),
        };
    }

    private string GetSunriseStorytellerTypeName(string presetId)
    {
        return presetId switch
        {
            StorytellerPresetHelper.StorytellerCalmId => "ui-vote-storyteller-type-calm-name",
            StorytellerPresetHelper.StorytellerClassicId => "ui-vote-storyteller-type-classic-name",
            StorytellerPresetHelper.StorytellerInsaneId => "ui-vote-storyteller-type-insane-name",
            _ => presetId,
        };
    }
}
