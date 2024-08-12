using System.Text.Json.Serialization;
using Content.Shared.Chemistry.Reagent;
using Content.Shared._Sunrise.Aphrodesiac;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class LoveEffect : EntityEffect
{
    [DataField]
    public float EffectPower = 3f;
    [DataField]
    [JsonPropertyName("scaleByQuantity")]
    public bool ScaleByQuantity;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-love");

    public override void Effect(EntityEffectBaseArgs args)
    {
        var effectPower = EffectPower;
        var scale = FixedPoint2.New(1);

        if (args is EntityEffectReagentArgs reagentArgs)
        {
            scale = ScaleByQuantity ? reagentArgs.Quantity * reagentArgs.Scale : reagentArgs.Scale;
        }

        effectPower *= (float) scale;

        var loveVisionSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedAphrodesiacSystem>();
        loveVisionSys.TryApplyLoveenness(args.TargetEntity, effectPower);
    }
}
