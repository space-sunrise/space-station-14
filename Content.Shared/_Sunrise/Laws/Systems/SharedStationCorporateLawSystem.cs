using System.Linq;
using Content.Shared._Sunrise.Laws.Components;
using Content.Shared.Station;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Laws.Systems;

public abstract class SharedStationCorporateLawSystem : EntitySystem
{
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] protected readonly IPrototypeManager _proto = default!;
    [Dependency] protected readonly IConfigurationManager _config = default!;

    public Entity<StationCorporateLawComponent>? GetStationLawset(EntityUid uid)
    {
        var station = _station.GetOwningStation(uid);
        if (station == null || !TryComp<StationCorporateLawComponent>(station, out var component))
            return null;

        return (station.Value, component);
    }

    /// <summary>
    ///     Gets the laws from the station component.
    ///     If the component is missing or has no articles/provisions, falls back to the default lawset from CVar.
    /// </summary>
    public void GetEffectiveLawset(NetEntity? netUid,
        out List<ProtoId<CorporateLawPrototype>> provisions,
        out List<ProtoId<CorporateLawSectionPrototype>> articles,
        out List<ProtoId<CorporateLawPrototype>> circumstances,
        out int permanentThreshold)
    {
        var uid = EntityManager.GetEntity(netUid ?? NetEntity.Invalid);
        GetEffectiveLawset(uid.Valid ? uid : null, out provisions, out articles, out circumstances, out permanentThreshold);
    }

    public void GetEffectiveLawset(EntityUid? uid,
        out List<ProtoId<CorporateLawPrototype>> provisions,
        out List<ProtoId<CorporateLawSectionPrototype>> articles,
        out List<ProtoId<CorporateLawPrototype>> circumstances,
        out int permanentThreshold)
    {
        StationCorporateLawComponent? comp = null;
        if (uid != null)
        {
            var station = _station.GetOwningStation(uid.Value);
            TryComp(station, out comp);
        }

        // If component exists and has data, use it
        if (comp != null && (comp.Articles.Count > 0 || comp.Provisions.Count > 0))
        {
            provisions = comp.Provisions;
            articles = comp.Articles;
            circumstances = comp.Circumstances;
            permanentThreshold = comp.PermanentSentenceThreshold;
            return;
        }

        // Fallback to CVar
        var lawsetId = _config.GetCVar(Content.Shared._Sunrise.SunriseCCVars.SunriseCCVars.CorporateLawSet);
        if (_proto.TryIndex<CorporateLawsetPrototype>(lawsetId, out var proto))
        {
            provisions = proto.Provisions;
            articles = proto.Articles;
            circumstances = proto.Circumstances;
            permanentThreshold = proto.PermanentSentenceThreshold;
        }
        else
        {
            provisions = new();
            articles = new();
            circumstances = new();
            permanentThreshold = 50;
        }
    }

    public bool IsLawInLawset(string lawId, StationCorporateLawComponent component)
    {
        // Check Articles
        foreach (var sectionId in component.Articles)
        {
            if (_proto.TryIndex(sectionId, out var section) && section.Entries.Contains(lawId))
                return true;
        }

        // Check Provisions
        if (component.Provisions.Any(p => (string) p == lawId))
            return true;

        return false;
    }

    public bool IsCircumstanceInLawset(string circId, StationCorporateLawComponent component)
    {
        return component.Circumstances.Contains(circId);
    }
}
