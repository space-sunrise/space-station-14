using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Research.Artifact.Effects.StartGamerule;

[RegisterComponent]
public sealed partial class ArtifactStartGameRuleComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, int> Rules = new ();
}
