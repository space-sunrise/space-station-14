using Content.Shared.Examine;

namespace Content.Server._Sunrise.Research.Artifact.Effects.ModifyHunger;

[RegisterComponent]
public sealed partial class ArtifactModifyHungerComponent : Component
{
    [DataField]
    public float Range = ExamineSystemShared.ExamineRange;

    [DataField]
    public float Amount = 40f;

    [DataField]
    public float MinModifier = -1.5f;

    [DataField]
    public float MaxModifier = 1.5f;
}
