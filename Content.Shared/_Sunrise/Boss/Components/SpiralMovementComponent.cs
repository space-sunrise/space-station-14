using Robust.Shared.Map;

namespace Content.Shared._Sunrise.Boss.Components;

[RegisterComponent]
public sealed partial class SpiralMovementComponent : Component
{
    [DataField]
    public EntityCoordinates? OriginCoordinates;

    [DataField]
    public TimeSpan RadiusCoefficient = TimeSpan.FromSeconds(0.3);

    [DataField]
    public TimeSpan TimeOffset = TimeSpan.Zero;

    [DataField]
    public float StartingRadius;

    [DataField]
    public float OmegaCoefficient = 2f * (float)Math.PI;

    [DataField]
    public float Offset;

    [DataField(readOnly: true)]
    public TimeSpan? SpawnTime;
}
