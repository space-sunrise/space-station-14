namespace Content.Shared.Ligyb;

[RegisterComponent]
public sealed partial class DiseaseImmuneClothingComponent : Component
{
    [DataField] public float prob;
    [DataField] public bool IsActive;
}
