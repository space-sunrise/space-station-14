using Content.Server.Shuttles.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Added to a station that is available for arrivals shuttles.
/// </summary>
[RegisterComponent, Access(typeof(ArrivalsSystem))]
public sealed partial class StationArrivalsComponent : Component
{
    public List<EntityUid> Shuttles = [];

    [DataField("shuttlePath")] public ResPath ShuttlePath = new("/Maps/Shuttles/arrivals.yml");
}
