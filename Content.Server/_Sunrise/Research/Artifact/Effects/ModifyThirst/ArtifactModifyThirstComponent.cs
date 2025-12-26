namespace Content.Server._Sunrise.Research.Artifact.Effects.ModifyThirst;

[RegisterComponent]
public sealed partial class ArtifactModifyThirstComponent : Component
{
    [DataField] public float Range = 12f;
    [DataField] public float Amount = 40f;
}
