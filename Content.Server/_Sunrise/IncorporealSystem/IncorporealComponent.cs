namespace Content.Server._Sunrise.IncorporealSystem;

[RegisterComponent]
public sealed partial class IncorporealComponent : Component
{
    [DataField("movementSpeedBuff")]
    public float MovementSpeedBuff = 1.5f;
}
