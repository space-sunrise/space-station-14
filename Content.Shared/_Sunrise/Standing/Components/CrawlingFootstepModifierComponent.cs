using Content.Shared._Sunrise.Standing.Systems;

namespace Content.Shared._Sunrise.Standing.Components;

[RegisterComponent, Access(typeof(SharedSunriseStandingStateSystem))]
public sealed partial class CrawlingFootstepModifierComponent : Component
{
    /// <summary>
    /// Whether the entity originally had the footstep tag before crawling muted it.
    /// </summary>
    public bool HadFootstepSoundTag;
}
