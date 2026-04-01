using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.EntityEffects.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class RandomSpeciesChange : EntityEffectBase<RandomSpeciesChange>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-random-species-change");
}
