using Content.Shared.Chemistry.Reagent;
using Content.Shared._Sunrise.Aphrodesiac;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects;

public sealed partial class LoveEffect : ReagentEffect
{
    [DataField]
    public float EffectPower = 3f;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-love");

    public override void Effect(ReagentEffectArgs args)
    {
        var effectPower = EffectPower;

        effectPower *= args.Scale;

        var loveVisionSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedAphrodesiacSystem>();
        loveVisionSys.TryApplyLoveenness(args.SolutionEntity, effectPower);
    }
}
