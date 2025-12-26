using System.Numerics;

namespace Content.Server._Sunrise.Cleaning;

[RegisterComponent]
public sealed partial class FootprintAreaCleanerComponent : Component
{
    [DataField]
    public bool Enabled = true;

    [DataField]
    public float Interval = 0.5f;

    [DataField]
    public Vector2 LastStepPosition = Vector2.Zero;
}
