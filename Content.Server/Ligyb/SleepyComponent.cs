using System.Numerics;

namespace Content.Server.Traits.Assorted;

[RegisterComponent, Access(typeof(NarcolepsySystem), typeof(SleepySystem))]
public sealed partial class SleepyComponent : Component
{
    [DataField("timeBetweenIncidents", required: true)]
    public Vector2 TimeBetweenIncidents = new Vector2(300, 600);

    [DataField("durationOfIncident", required: true)]
    public Vector2 DurationOfIncident = new Vector2(10, 30);

    public float NextIncidentTime;
}
