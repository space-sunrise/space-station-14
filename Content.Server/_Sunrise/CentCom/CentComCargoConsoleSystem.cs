using Content.Server._Sunrise.StationCentComm;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.CentCom;
using Content.Shared._Sunrise.CentCom.BUIStates;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.CentCom;

public sealed class CentComCargoConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CentComCargoConsoleComponent, ComponentInit>(OnCargoInit);
        SubscribeLocalEvent<CentComCargoConsoleComponent, CentComCargoSendGiftMessage>(OnSendGift);
    }

    private void OnCargoInit(EntityUid uid, CentComCargoConsoleComponent component, ComponentInit args)
    {

    }

    private void OnSendGift(EntityUid uid, CentComCargoConsoleComponent component, CentComCargoSendGiftMessage args)
    {
        if (!_prototypeMan.HasIndex(args.TargetGift))
            return;
        var ruleUid = _ticker.AddGameRule(args.TargetGift);
    }

    private void UpdateStation(EntityUid uid, CentComCargoConsoleComponent component)
    {
        var uUid = _station.GetStationInMap(_transform.GetMapId(uid));
        if (uUid == null)
            return;
        if (!TryComp<CentCommStationComponent>(uUid, out var centCommStationComponent))
            return;

        var stationUid = centCommStationComponent.ParentStation;

        var data = new CargoLinkedStation()
        {
            Uid = GetNetEntity(stationUid),
        };

        component.LinkedStation = data;
    }

    private void UpdateUi(EntityUid uid, CentComCargoConsoleComponent component, EntityEventArgs args)
    {
        UpdateStation(uid, component);

        var newState = new CentComCargoConsoleBoundUserInterfaceState(GetNetEntity(uid), component.LinkedStation);

        _userInterface.SetUiState(uid, CentComCargoConsoleUiKey.Key, newState);
    }
}
