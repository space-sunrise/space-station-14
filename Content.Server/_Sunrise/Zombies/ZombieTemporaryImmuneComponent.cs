namespace Content.Server._Sunrise.Zombies;

/// <summary>
/// Entities with this component are temporarily immune to zombification.
/// </summary>
/// <param name="expiryTime">Time when immunity expires.</param>
[RegisterComponent]
public sealed partial class ZombieTemporaryImmuneComponent : Component
{
    public ZombieTemporaryImmuneComponent(TimeSpan expiryTime)
    {
        ExpiryTime = expiryTime;
    }

    [DataField]
    public TimeSpan ExpiryTime;
}
