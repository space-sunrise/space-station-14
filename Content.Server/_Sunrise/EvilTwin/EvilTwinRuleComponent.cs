namespace Content.Server._Sunrise.EvilTwin;

[RegisterComponent, Access(typeof(EvilTwinSystem))]
public sealed partial class EvilTwinRuleComponent : Component
{
    public List<(EntityUid, string)> TwinsMinds = new();
}
