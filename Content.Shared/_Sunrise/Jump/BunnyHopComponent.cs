using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Jump;

[RegisterComponent, NetworkedComponent]
public sealed partial class BunnyHopComponent : Component
{
    public TimeSpan LastLandingTime = TimeSpan.Zero;
    public float SpeedMultiplier = 1.0f;

    public bool CanBunnyHop => SpeedMultiplier > 1.0f;
};
