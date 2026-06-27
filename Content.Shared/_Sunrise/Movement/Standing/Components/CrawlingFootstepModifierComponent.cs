using Content.Shared._Sunrise.Movement.Standing.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Movement.Standing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSunriseStandingStateSystem))]
public sealed partial class CrawlingFootstepModifierComponent : Component
{
    /// <summary>
    /// Whether the entity originally had the footstep tag before crawling muted it.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool HadFootstepSoundTag;
}
