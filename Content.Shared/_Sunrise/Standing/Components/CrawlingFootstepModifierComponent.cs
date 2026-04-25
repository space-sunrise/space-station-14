using Content.Shared._Sunrise.Standing.Systems;
using Robust.Shared.Audio;

namespace Content.Shared._Sunrise.Standing.Components;

[RegisterComponent, Access(typeof(SharedSunriseStandingStateSystem))]
public sealed partial class CrawlingFootstepModifierComponent : Component
{
    /// <summary>
    /// Whether the entity had its own footstep modifier before it started crawling.
    /// </summary>
    public bool HadFootstepModifier;

    /// <summary>
    /// Footstep sound that should be restored after crawling ends.
    /// </summary>
    public SoundSpecifier? OriginalSound;

    /// <summary>
    /// Crawling sound currently applied by <see cref="SharedSunriseStandingStateSystem"/>.
    /// </summary>
    public SoundSpecifier? AppliedSound;
}
