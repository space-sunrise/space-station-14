namespace Content.Server._Sunrise.Research.Artifact.Effects.BoltAirlocks;

[RegisterComponent]
public sealed partial class ArtifactBoltAirlocksComponent : Component
{
    [DataField] public float Range = 12f;
    [DataField] public float Chance = 70f;
}
