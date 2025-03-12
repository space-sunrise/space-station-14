namespace Content.Server._Sunrise.Pibble;

[RegisterComponent]
public sealed partial class AttackOnInteractionFailComponent : Component
{
    [DataField("attackMemoryLength"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? AttackMemoryLength;

    [DataField("attackMemories")]
    public Dictionary<EntityUid, TimeSpan> AttackMemories = new();
}
