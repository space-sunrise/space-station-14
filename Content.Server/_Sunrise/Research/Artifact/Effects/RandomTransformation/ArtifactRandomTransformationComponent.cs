using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Research.Artifact.Effects.RandomTransformation;

[RegisterComponent]
public sealed partial class ArtifactRandomTransformationComponent : Component
{
    [DataField, ViewVariables]
    public float TransformationPercentRatio = 20f;

    [DataField, ViewVariables]
    public float Radius = 12f;

    [DataField]
    public HashSet<EntProtoId>? PrototypeBlacklist;

    [DataField]
    public HashSet<ProtoId<EntityCategoryPrototype>>? CategoryBlacklist;

    [DataField]
    public HashSet<string>? ComponentBlacklist;

    [DataField]
    public HashSet<EntProtoId>? PrototypeBlacklistExceptions;
}
