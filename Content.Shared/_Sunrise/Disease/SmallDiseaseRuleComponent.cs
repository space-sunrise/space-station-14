using Content.Shared.GameTicking.Components;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Disease;

[RegisterComponent]
public sealed partial class SmallDiseaseRuleComponent : Component
{
    [DataField]
    public int TargetInfectedCount = 3;

    [DataField]
    public int TargetSymptomPoints = 50;

    [DataField]
    public EntProtoId DiseasePrototype = "MobDisease";

    [DataField]
    public string SymptomCategory = "DiseaseSymptomsCategory";

    [DataField]
    public ProtoId<ListingPrototype> DefaultSymptom = "Cough";
}

