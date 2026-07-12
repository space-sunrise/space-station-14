using System.Linq;
using Content.Server._Sunrise.Presets;
using Content.Server.GameTicking.Presets;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Storyteller.Systems;

public sealed partial class StorytellerSystem
{
    public const string StorytellerClassicId = "StorytellerClassic";
    public const string StorytellerCalmId = "StorytellerCalm";
    public const string StorytellerInsaneId = "StorytellerInsane";

    private static readonly ProtoId<GamePresetPoolPrototype> StorytellerPoolPrototypeId = "StorytellerPresetPool";

    public Dictionary<string, string> GetAvailableVotePresets()
    {
        var playerCount = _playerManager.PlayerCount;
        var excludedPresets = GameTicker.ExcludedPresets.ToHashSet();
        var result = new Dictionary<string, string>();
        var storytellerPool = new Dictionary<string, int[]>();

        AdjustPresetPool(storytellerPool);

        foreach (var (presetId, limits) in storytellerPool)
        {
            if (excludedPresets.Contains(presetId))
                continue;

            if (!_protoManager.TryIndex<GamePresetPrototype>(presetId, out var preset))
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

    public void AdjustPresetPool(Dictionary<string, int[]> presets)
    {
        if (!_cfg.GetCVar(SunriseCCVars.StorytellerEnabled))
            return;

        if (!_protoManager.TryIndex(StorytellerPoolPrototypeId, out var poolPrototype))
        {
            Log.Error($"Storyteller preset pool '{StorytellerPoolPrototypeId}' not found!");
            return;
        }

        if (_cfg.GetCVar(SunriseCCVars.StorytellerOverridePresetPool))
            presets.Clear();

        foreach (var (presetId, limits) in poolPrototype.Presets)
        {
            presets.TryAdd(presetId, limits);
        }

        if (_cfg.GetCVar(SunriseCCVars.StorytellerRotationEnabled))
            ApplyRotationFilter(presets, _cfg.GetCVar(SunriseCCVars.StorytellerRotationCounter));
    }

    public static bool IsStorytellerPreset(string presetId)
    {
        return presetId is StorytellerClassicId or StorytellerCalmId or StorytellerInsaneId;
    }

    public static string GetVoteOptionLocId(string presetId)
    {
        return presetId switch
        {
            StorytellerCalmId => "ui-vote-storyteller-type-calm",
            StorytellerClassicId => "ui-vote-storyteller-type-classic",
            StorytellerInsaneId => "ui-vote-storyteller-type-insane",
            _ => GetNameLocId(presetId),
        };
    }

    public static string GetNameLocId(string presetId)
    {
        return presetId switch
        {
            StorytellerCalmId => "ui-vote-storyteller-type-calm-name",
            StorytellerClassicId => "ui-vote-storyteller-type-classic-name",
            StorytellerInsaneId => "ui-vote-storyteller-type-insane-name",
            _ => presetId,
        };
    }

    private void ApplyRotationFilter(Dictionary<string, int[]> presets, int state)
    {
        if (state == 1)
        {
            presets.Remove(StorytellerInsaneId);
            return;
        }

        if (state != 2 || !presets.ContainsKey(StorytellerCalmId))
            return;

        foreach (var (presetId, limits) in presets)
        {
            if (presetId == StorytellerCalmId)
                continue;

            var minPlayers = limits.Length > 0 ? limits[0] : int.MinValue;
            var maxPlayers = limits.Length > 1 ? limits[1] : int.MaxValue;

            if (_playerManager.PlayerCount < minPlayers || _playerManager.PlayerCount > maxPlayers)
                continue;

            presets.Remove(StorytellerCalmId);
            return;
        }
    }
}
