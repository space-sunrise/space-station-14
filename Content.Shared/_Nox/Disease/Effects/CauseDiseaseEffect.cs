// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared._Nox.Disease.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nox.Disease.Effects;

public sealed partial class CauseDiseaseEffect : EntityEffectBase<CauseDiseaseEffect>
{
    [DataField]
    public DiseaseData Data = new();
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-disease");
}
