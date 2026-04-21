using Content.Shared._Sunrise.Laws.Components;
using Content.Shared.Station;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Laws.Systems;

public abstract class SharedStationCorporateLawSystem : EntitySystem
{
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public Entity<StationCorporateLawComponent>? GetStationLawset(EntityUid uid)
    {
        var station = _station.GetOwningStation(uid);
        if (station == null || !TryComp<StationCorporateLawComponent>(station, out var component))
            return null;

        return (station.Value, component);
    }

    public bool IsLawInLawset(string lawId, StationCorporateLawComponent component)
    {
        foreach (var sectionId in component.Articles)
        {
            if (_proto.TryIndex(sectionId, out var section) && section.Entries.Contains(lawId))
                return true;
        }
        return false;
    }

    public bool IsCircumstanceInLawset(string circId, StationCorporateLawComponent component)
    {
        return component.Circumstances.Contains(circId);
    }
}
