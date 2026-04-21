using Content.Shared._Sunrise.Laws;
using Content.Shared._Sunrise.Laws.Components;
using Content.Shared._Sunrise.Laws.Systems;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Server.Station.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Laws.Systems;

public sealed class StationCorporateLawSystem : SharedStationCorporateLawSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized);
    }

    private void OnStationInitialized(StationInitializedEvent args)
    {
        InitializeLawset(args.Station);
    }

    private void InitializeLawset(EntityUid station)
    {
        var component = EnsureComp<StationCorporateLawComponent>(station);

        var lawsetId = _cfg.GetCVar(SunriseCCVars.CorporateLawSet);
        if (!_proto.TryIndex<CorporateLawsetPrototype>(lawsetId, out var prototype))
            return;

        component.Provisions = new(prototype.Provisions);
        component.Circumstances = new(prototype.Circumstances);
        component.Articles = new(prototype.Articles);
        component.PermanentSentenceThreshold = prototype.PermanentSentenceThreshold;
        component.LawsetPrototype = lawsetId;

        Dirty(station, component);
    }
}
