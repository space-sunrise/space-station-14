using Content.Server._Sunrise.ImmortalGrid;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Shuttles.Components;
using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Shuttles;

public sealed class CodeEquipmentSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly ShuttleSystem _shuttles = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _nav = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CodeEquipmentComponent, StationPostInitEvent>(OnStationPostInit);
        SubscribeLocalEvent<CodeEquipmentShuttleComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<CodeEquipmentShuttleComponent, FTLTagEvent>(OnFTLShuttleTag);
        SubscribeLocalEvent<CodeEquipmentShuttleComponent, FTLStartedEvent>(OnFTLStartedEvent);
        SubscribeLocalEvent<CodeEquipmentShuttleComponent, FTLCompletedEvent>(OnFTLCompletedEvent);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }

    private void OnStationPostInit(EntityUid uid, CodeEquipmentComponent comp, StationPostInitEvent ev)
    {
        var map = _mapManager.CreateMap();
        var loadOptions = new MapLoadOptions();
        loadOptions.LoadMap = true;
        loadOptions.StoreMapUids = true;
        _loader.TryLoad(map, comp.ShuttlePath.ToString(), out var shuttleUids, loadOptions);
        if (shuttleUids is null)
            return;
        comp.Shuttles.Add(shuttleUids[0]);
        var gammaArmoryComp = EnsureComp<CodeEquipmentShuttleComponent>(shuttleUids[0]);
        gammaArmoryComp.Station = uid;
    }

    private void OnFTLShuttleTag(EntityUid uid, CodeEquipmentShuttleComponent comp, ref FTLTagEvent ev)
    {
        if (ev.Handled)
            return;

        ev.Handled = true;
        ev.Tag = comp.PriorityTag;
    }

    private void OnComponentStartup(EntityUid uid, CodeEquipmentShuttleComponent comp, ComponentStartup ev)
    {
        EnsureComp<PreventPilotComponent>(uid);
    }

    private void OnFTLStartedEvent(EntityUid uid, CodeEquipmentShuttleComponent comp, ref FTLStartedEvent ev)
    {

    }

    private void OnFTLCompletedEvent(EntityUid uid, CodeEquipmentShuttleComponent comp, ref FTLCompletedEvent ev)
    {
        if (comp.EnableDockAnnouncement)
        {
            var xform = Transform(uid);
            _chat.DispatchGlobalAnnouncement(
                Loc.GetString(comp.DockAnnounceMessage, ("location", FormattedMessage.RemoveMarkup(_nav.GetNearestBeaconString((uid, xform))))),
                colorOverride: Color.PaleVioletRed,
                announceVoice: "Azir");
        }
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent ev)
    {
        if (!TryComp<CodeEquipmentComponent>(ev.Station, out var comp))
            return;

        if (ev.AlertLevel != comp.TargetCode)
            return;

        var target = _station.GetLargestGrid(Comp<StationDataComponent>(ev.Station));

        if (target == null)
            return;

        _shuttles.FTLToDock(
            comp.Shuttles[0],
            Comp<ShuttleComponent>(comp.Shuttles[0]),
            target.Value,
            priorityTag: comp.PriorityTag,
            ignored: true);
    }
}
