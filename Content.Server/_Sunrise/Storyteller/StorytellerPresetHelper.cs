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

    public static void AdjustPresetPool(Dictionary<string, int[]> presets, IConfigurationManager cfg)
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
            ApplyRotationFilter(presets, state);
        }
    }

    private static void ApplyRotationFilter(Dictionary<string, int[]> presets, int state)
    {
        if (state == 1)
        {
            presets.Remove(StorytellerInsaneId);
        }
    }

    public static bool ShouldBypassExclusion(string presetId)
    {
        return presetId == StorytellerClassicId || presetId == StorytellerCalmId || presetId == StorytellerInsaneId;
    }
}
