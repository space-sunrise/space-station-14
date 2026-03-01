using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Disease.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseInfectionCloudComponent : Component
{
    [DataField]
    public float InfectionChance = 0.3f;

    [DataField]
    public int SpreadAmount = 4;

    [DataField]
    public EntProtoId CloudPrototype = "SunriseDiseaseInfectionCloud";

    [ViewVariables]
    public DiseaseData? Data;

    [ViewVariables]
    public EntityUid? Source;

    public DiseaseInfectionCloudComponent(DiseaseData disease)
    {
        Data = disease;
    }

    public DiseaseInfectionCloudComponent()
    {
        Data = new DiseaseData();
    }
}

[Serializable, NetSerializable]
public sealed class DiseaseInfectionCloudComponentState : ComponentState
{
    public Color Color;

    public DiseaseInfectionCloudComponentState(Color color)
    {
        Color = color;
    }
}