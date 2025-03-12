namespace Content.Server._Sunrise.FleshCult.FleshGrowth;

[RegisterComponent, Access(typeof(SpreaderFleshSystem), typeof(FleshCultSystem))]
public sealed partial class SpreaderFleshComponent : Component
{
    [DataField("chance", required: true)]
    public float Chance = 1f;

    [DataField("growthResult", required: true)]
    public string GrowthResult = "Flesh";

    [DataField("wallResult", required: true)]
    public string WallResult = "WallFlesh";

    [DataField("enabled")]
    public bool Enabled = true;

    [DataField("source")]
    public EntityUid? Source;
}
