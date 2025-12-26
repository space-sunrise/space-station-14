namespace Content.Server._Sunrise.Heartbeat.Components;

[RegisterComponent]
public sealed partial class ActiveHeartbeatComponent : Component
{
    [ViewVariables] public float Pitch = 1f;
    [ViewVariables] public TimeSpan NextHeartbeatCooldown = TimeSpan.FromSeconds(0.5f);

    public TimeSpan? NextHeartbeatTime;
}
