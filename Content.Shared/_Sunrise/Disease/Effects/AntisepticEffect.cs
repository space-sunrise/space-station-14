// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Disease.Effects;

public sealed partial class AntisepticEffect : EntityEffectBase<AntisepticEffect>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-antiseptic");
}
