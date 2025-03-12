using Content.Server._Sunrise.BloodCult.Objectives.Systems;

namespace Content.Server._Sunrise.BloodCult.Objectives.Components;

[RegisterComponent, Access(typeof(KillCultistTargetsConditionSystem))]
public sealed partial class KillCultistTargetsConditionComponent : Component
{
    public List<EntityUid> Targets = new();

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Title = string.Empty;
}
