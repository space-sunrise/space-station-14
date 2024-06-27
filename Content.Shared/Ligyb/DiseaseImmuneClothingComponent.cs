namespace Content.Shared.Ligyb;

[RegisterComponent]
public sealed partial class DiseaseImmuneClothingComponent : Component
{
    [DataField] public float Prob;
    [DataField] public bool IsActive;
}
