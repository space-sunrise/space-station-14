namespace Content.Server._Sunrise.FleshCult.Events;

[RegisterComponent, Access(typeof(VentFleshWormsRule))]
public sealed partial class VentFleshWormsRuleComponent : Component
{
    [DataField("spawnedPrototypeWorm")]
    public string SpawnedPrototypeWorm = "MobFleshWorm";
}
