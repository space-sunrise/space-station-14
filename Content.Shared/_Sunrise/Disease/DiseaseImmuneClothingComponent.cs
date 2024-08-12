// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
namespace Content.Shared._Sunrise.Disease;

[RegisterComponent]
public sealed partial class DiseaseImmuneClothingComponent : Component
{
    [DataField] public float Prob;
    [DataField] public bool IsActive;
}
