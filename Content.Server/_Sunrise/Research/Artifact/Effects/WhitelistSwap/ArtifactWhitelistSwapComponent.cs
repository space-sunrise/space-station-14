using Content.Shared.Whitelist;

namespace Content.Server._Sunrise.Research.Artifact.Effects.WhitelistSwap;

[RegisterComponent]
public sealed partial class ArtifactWhitelistSwapComponent : Component
{
    [DataField(required: true)]
    public EntityWhitelist TargetWhitelist;
}
