using Content.Shared._Sunrise.Laws;
using Content.Shared._Sunrise.Laws.Components;
using Content.Shared._Sunrise.Laws.Systems;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Laws.Systems;

public sealed class StationCorporateLawSystem : SharedStationCorporateLawSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationCorporateLawComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, StationCorporateLawComponent component, ComponentStartup args)
    {
        // If we are on a station and have no articles/provisions, try to initialze from CVar.
        // This acts as a prediction/fallback until the server's state syncs.
        if (component.Articles.Count > 0 || component.Provisions.Count > 0)
            return;

        if (!HasComp<StationDataComponent>(uid))
            return;

        var lawsetId = _config.GetCVar(SunriseCCVars.CorporateLawSet);
        if (!_proto.TryIndex<CorporateLawsetPrototype>(lawsetId, out var prototype))
            return;

        component.Provisions = new(prototype.Provisions);
        component.Articles = new(prototype.Articles);
        component.Circumstances = new(prototype.Circumstances);
        component.PermanentSentenceThreshold = prototype.PermanentSentenceThreshold;
    }
}
