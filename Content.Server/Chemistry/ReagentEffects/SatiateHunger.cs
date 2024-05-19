using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Attempts to find a HungerComponent on the target,
    /// and to update it's hunger values.
    /// </summary>
    public sealed partial class SatiateHunger : ReagentEffect
    {
        private const float DefaultNutritionFactor = 3.0f;

        /// <summary>
        ///     How much hunger is satiated when 1u of the reagent is metabolized
        /// </summary>
        [DataField("factor")] public float NutritionFactor { get; set; } = DefaultNutritionFactor;

        //Remove reagent at set rate, satiate hunger if a HungerComponent can be found
        public override void Effect(ReagentEffectArgs args)
        {
            var entman = args.EntityManager;
            if (!entman.TryGetComponent(args.SolutionEntity, out SatiationComponent? satiation))
                return;
            entman.System<SatiationSystem>().ModifyHunger((args.SolutionEntity, satiation), NutritionFactor * (float) args.Quantity);
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-satiate-hunger", ("chance", Probability), ("relative", NutritionFactor / DefaultNutritionFactor));
    }
}
