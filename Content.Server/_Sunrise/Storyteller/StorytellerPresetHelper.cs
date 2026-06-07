using Content.Server._Sunrise.Presets;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Storyteller;

public static class StorytellerPresetHelper
{
    public static readonly string StorytellerClassicId = "StorytellerClassic";
    public static readonly string StorytellerCalmId = "StorytellerCalm";
    public static readonly string StorytellerInsaneId = "StorytellerInsane";

    private static readonly ProtoId<GamePresetPoolPrototype> PoolPrototypeId = "StorytellerPresetPool";

    public static void AdjustPresetPool(Dictionary<string, int[]> presets, IConfigurationManager cfg, int playerCount)
    {
        if (!cfg.GetCVar(SunriseCCVars.StorytellerEnabled))
            return;

        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        if (!protoMan.TryIndex(PoolPrototypeId, out var poolProto))
        {
            var sawmill = Logger.GetSawmill("storyteller");
            sawmill.Error($"Preset pool '{PoolPrototypeId}' not found!");
            return;
        }

        var overridePool = cfg.GetCVar(SunriseCCVars.StorytellerOverridePresetPool);
        var rotationEnabled = cfg.GetCVar(SunriseCCVars.StorytellerRotationEnabled);

        if (overridePool)
        {
            presets.Clear();
        }

        foreach (var (key, limits) in poolProto.Presets)
        {
            presets.TryAdd(key, limits);
        }

        if (rotationEnabled)
        {
            var state = cfg.GetCVar(SunriseCCVars.StorytellerRotationCounter);
            ApplyRotationFilter(presets, state, playerCount);
        }
    }

    private static void ApplyRotationFilter(Dictionary<string, int[]> presets, int state, int playerCount)
    {
        // State meanings:
        // 0 - no special exclusions
        // 1 - Insane was played previously -> put Insane on cooldown
        // 2 - Calm was played previously -> put Calm on cooldown (with safety checks)

        if (state == 1)
        {
            presets.Remove(StorytellerInsaneId);
            return;
        }

        if (state == 2)
        {
            if (!presets.ContainsKey(StorytellerCalmId))
                return;

            var remainingEligible = 0;
            foreach (var (key, limits) in presets)
            {
                if (key == StorytellerCalmId)
                    continue;

                var minPlayers = limits.Length > 0 ? limits[0] : int.MinValue;
                var maxPlayers = limits.Length > 1 ? limits[1] : int.MaxValue;

                if (playerCount >= minPlayers && playerCount <= maxPlayers)
                    remainingEligible++;
            }

            if (remainingEligible > 0)
            {
                presets.Remove(StorytellerCalmId);
            }
        }
    }

    public static bool ShouldBypassExclusion(string presetId)
    {
        return presetId == StorytellerClassicId;
    }
}
