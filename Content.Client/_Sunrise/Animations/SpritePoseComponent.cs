namespace Content.Client._Sunrise.Animations;

[RegisterComponent, Access(typeof(SpritePoseSystem))]
public sealed partial class SpritePoseComponent : Component
{
    public bool HasPose;
    public Angle Rotation;
    public Angle BaseRotation;
    public Angle? OverrideRotation;
}
