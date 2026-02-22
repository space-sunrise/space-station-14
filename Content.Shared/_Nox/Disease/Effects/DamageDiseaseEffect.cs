// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nox.Disease.Effects;

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class DamageDiseaseEffect : EntityEffectBase<DamageDiseaseEffect>
{
    [DataField]
    public float BaseDamage = 1f;
    [DataField]
    public float ResistanceIncrease = 0.05f;

    /// <summary>
    ///     Ключ по которому добавляется сопротивление болезни к этому веществу.
    ///     Может быть произвольным, или же совпадать с ID реагента, если нужно, чтобы сопротивление росло только от этого реагента.
    /// </summary>
    [DataField]
    public string Key = "базовый";

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-damage-disease",
            ("baseDamage", BaseDamage),
            ("resistanceIncrease", ResistanceIncrease)
        );
}
