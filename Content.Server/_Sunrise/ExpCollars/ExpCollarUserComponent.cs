namespace Content.Server._Sunrise.ExpCollars;

[RegisterComponent]
public sealed partial class ExpCollarUserComponent : Component
{
    [DataField(readOnly: true)]
    public EntityUid? Tool;
}
