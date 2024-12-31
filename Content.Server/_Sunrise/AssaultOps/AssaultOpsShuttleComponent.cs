namespace Content.Server._Sunrise.AssaultOps;

[RegisterComponent]
public sealed partial class AssaultOpsShuttleComponent : Component
{
    [DataField]
    public EntityUid AssociatedRule;
}
