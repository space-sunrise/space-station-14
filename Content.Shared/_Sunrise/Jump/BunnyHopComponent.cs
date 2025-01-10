using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Jump;

[NetworkedComponent, RegisterComponent]
public sealed partial class BunnyHopComponent  : Component
{
    public TimeSpan LastLandingTime { get; set; } = TimeSpan.Zero;
    public float SpeedMultiplier { get; set; } = 1.0f;
    public bool CanBunnyHop => SpeedMultiplier > 1.0f;
};
