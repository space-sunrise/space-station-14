using Content.Shared.Examine;

namespace Content.Server._Sunrise.Research.Artifact.Effects.ModifyThirst;

[RegisterComponent]
public sealed partial class ArtifactModifyThirstComponent : Component
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
