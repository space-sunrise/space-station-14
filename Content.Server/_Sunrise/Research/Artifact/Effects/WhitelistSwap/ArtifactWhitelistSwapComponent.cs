using Content.Shared.Whitelist;

namespace Content.Server._Sunrise.Research.Artifact.Effects.WhitelistSwap;

[RegisterComponent]
public sealed partial class ArtifactWhitelistSwapComponent : Component
{
    [DataField]
    public EntityWhitelist? TargetWhitelist;

    [DataField]
    public EntityWhitelist? TargetBlacklist;

    [DataField]
    public bool PreventTeleportFromOtherMaps = true;

    [DataField]
    public EntityWhitelist? OtherMapWhitelist;

    [DataField]
    public EntityWhitelist? OtherMapBlacklist;
}
