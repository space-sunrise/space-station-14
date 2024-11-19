namespace Content.Server._Sunrise.Fugitive;

[RegisterComponent, Access(typeof(FugitiveSystem))]
public sealed partial class FugitiveRuleComponent : Component
{
    public List<(EntityUid, string)> FugitiveMinds = new();
}
