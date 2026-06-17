using Content.Shared.Whitelist;

namespace Content.Server._Sunrise.Objectives.Components;

[RegisterComponent]
public sealed partial class EnsureLawBoundEntitiesHaveNoLawsConditionComponent : Component
{
    /// <summary>
    /// Number of law-bound entities that must have no laws for success.
    /// </summary>
    [DataField]
    public int EntitiesToFree = 3;

    /// <summary>
    /// Optional whitelist of entities that can count toward progress.
    /// </summary>
    [DataField]
    public EntityWhitelist? LawEntityWhitelist;

    /// <summary>
    /// Optional blacklist of entities that should never count toward progress.
    /// </summary>
    [DataField]
    public EntityWhitelist? LawEntityBlacklist;
}
