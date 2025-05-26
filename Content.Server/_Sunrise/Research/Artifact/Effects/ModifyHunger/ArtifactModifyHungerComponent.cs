namespace Content.Server._Sunrise.Research.Artifact.Effects.ModifyHunger;

[RegisterComponent]
public sealed partial class ArtifactModifyHungerComponent : Component
{
    [DataField] public float Range = 12f;
    [DataField] public float Amount = 40f;
}
