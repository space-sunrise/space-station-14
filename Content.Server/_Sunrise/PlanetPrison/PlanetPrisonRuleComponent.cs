namespace Content.Server._Sunrise.PlanetPrison;

[RegisterComponent, Access(typeof(PlanetPrisonSystem))]
public sealed partial class PlanetPrisonRuleComponent : Component
{
    public List<(EntityUid, string)> PrisonersMinds = new();

    public List<EntityUid> EscapedPrisoners = new();
}
