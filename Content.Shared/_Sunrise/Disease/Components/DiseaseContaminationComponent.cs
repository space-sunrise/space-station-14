using Content.Shared._Sunrise.TimeWindow;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Disease.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DiseaseContaminationComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float Contamination;

    [ViewVariables(VVAccess.ReadWrite)]
    public Color Color = Color.Transparent;

    [ViewVariables(VVAccess.ReadOnly)]
    public DiseaseData? Data;

    [ViewVariables(VVAccess.ReadOnly)]
    public int StrongestSymptoms;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimedWindow SpreadWindow = new(TimeSpan.FromSeconds(1f), TimeSpan.FromSeconds(5f));

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float SpreadRange = 1f;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float CollisionContaminationGain = 0.35f;

    public DiseaseContaminationComponent(DiseaseData data)
    {
        Data = data;
    }

    public DiseaseContaminationComponent()
    {
        Data = null;
    }
}

[Serializable, NetSerializable]
public sealed class DiseaseContaminationComponentState : ComponentState
{
    public Color Color;
    public float Contamination;

    public DiseaseContaminationComponentState(Color color, float contamination)
    {
        Color = color;
        Contamination = contamination;
    }
}