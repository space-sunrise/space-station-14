using Content.Server._Sunrise.StationCentComm;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.CentCom;
using Content.Shared._Sunrise.CentCom.BUIStates;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.CentCom;

public sealed class CentComConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CentComConsoleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<CentComConsoleComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<CentComConsoleComponent, UnanchorAttemptEvent>(OnUnanchor);
        SubscribeLocalEvent<CentComConsoleComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<CentComConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUi);
        SubscribeLocalEvent<CentComConsoleComponent, EntRemovedFromContainerMessage>(UpdateUi);
        SubscribeLocalEvent<CentComConsoleComponent, BoundUIOpenedEvent>(UpdateUi);
    }

    private void OnComponentInit(EntityUid uid, CentComConsoleComponent component, ComponentInit args)
    {
        _itemSlotsSystem.AddItemSlot(uid, CentComConsoleComponent.IdCardSlotId, component.IdSlot);
    }

    private void OnComponentRemove(EntityUid uid, CentComConsoleComponent component, ComponentRemove args)
    {
        _itemSlotsSystem.RemoveItemSlot(uid, component.IdSlot);
    }

    private void UpdateStation(EntityUid uid, CentComConsoleComponent component)
    {
        var uUid = _station.GetStationInMap(_transform.GetMapId(uid));
        if (uUid == null)
            return;
        if (!TryComp<CentCommStationComponent>(uUid, out var centCommStationComponent))
            return;

        var stationUid = centCommStationComponent.ParentStation;

        var meta = MetaData(stationUid);
        if (!TryComp<AlertLevelComponent>(stationUid, out var alertLevelComponent))
            return;
        List<string> alertLevels = [];
        if (alertLevelComponent.AlertLevels != null)
        {
            foreach (var i in alertLevelComponent.AlertLevels.Levels)
            {
                alertLevels.Add(i.Key);
            }
        }
        component.Station = new LinkedStation()
        {
            Name = meta.EntityName,
            AlertLevels = alertLevels,
            CurrentAlert = alertLevelComponent.CurrentLevel,
            DefaultDelay = TimeSpan.FromMinutes(10),
        };
    }

    private void OnAnchorStateChanged(EntityUid uid, CentComConsoleComponent component, AnchorStateChangedEvent args)
    {
        if (args.Anchored == true)
        {
            UpdateStation(uid, component);
        }
    }

    private void OnUnanchor(EntityUid uid, CentComConsoleComponent component, UnanchorAttemptEvent args)
    {
        args.Cancel();
    }

    private void UpdateUi(EntityUid uid, CentComConsoleComponent component, EntityEventArgs args)
    {
        if (!component.Initialized)
            return;
        UpdateStation(uid, component);

        var idName = string.Empty;
        var idPresent = component.IdSlot.Item.HasValue;

        if (idPresent && component.IdSlot.Item != null)
        {
            idName = MetaData(component.IdSlot.Item.Value).EntityName;
        }
        var newState = new CentComConsoleBoundUserInterfaceState(idPresent, idName, component.Station, GetNetEntity(uid));

        _userInterface.SetUiState(uid, CentComConsoleUiKey.Key, newState);
    }
}
