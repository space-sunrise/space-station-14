using Content.Shared.GameTicking.Components;

namespace Content.Shared._Sunrise.Disease;

[RegisterComponent]
public sealed partial class SmallDiseaseRuleComponent : Component
{
    [DataField]
    public int TargetInfectedCount = 3;

    [DataField]
    public int TargetSymptomPoints = 50;
}
