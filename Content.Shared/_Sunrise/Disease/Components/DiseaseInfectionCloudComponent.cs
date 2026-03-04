using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Disease.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseInfectionCloudComponent : Component
{
    [DataField]
    public float InfectionChance = 0.3f;

    [DataField]
    public int SpreadAmount = 4;

    [ViewVariables]
    public DiseaseData? Data = null;

    [DataField]
    public bool NewData = false;

    [ViewVariables]
    public EntityUid? Source;

    public DiseaseInfectionCloudComponent(DiseaseData disease)
    {
        Data = disease;
    }

    public DiseaseInfectionCloudComponent()
    {
        Data = null;
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