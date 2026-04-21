using Content.Shared._Sunrise.Laws;
using Content.Shared._Sunrise.Laws.Components;
using Content.Shared._Sunrise.Laws.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.SunriseCCVars;
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
        var lawsetId = _cfg.GetCVar(SunriseCCVars.CorporateLawSet);
        if (!_proto.TryIndex<CorporateLawsetPrototype>(lawsetId, out var prototype))
            return;

        var component = EnsureComp<StationCorporateLawComponent>(station);

        component.Provisions = new List<ProtoId<CorporateLawPrototype>>(prototype.Provisions);
        component.Circumstances = new List<ProtoId<CorporateLawPrototype>>(prototype.Circumstances);
        component.Articles = new List<ProtoId<CorporateLawSectionPrototype>>(prototype.Articles);
        component.PermanentSentenceThreshold = prototype.PermanentSentenceThreshold;
        component.LawsetPrototype = lawsetId;

        Dirty(station, component);
    }
}
