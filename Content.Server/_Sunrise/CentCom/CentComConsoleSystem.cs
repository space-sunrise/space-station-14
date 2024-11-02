using Content.Server._Sunrise.StationCentComm;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.CentCom;
using Content.Shared._Sunrise.CentCom.BUIStates;
using Content.Shared.Construction.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.CentCom;

/// <summary>
/// This handles...
/// </summary>
public sealed class CentComConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CentComConsoleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CentComConsoleComponent, UnanchorAttemptEvent>(OnUnanchor);
        SubscribeLocalEvent<CentComConsoleComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<CentComConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUi);
        SubscribeLocalEvent<CentComConsoleComponent, EntRemovedFromContainerMessage>(UpdateUi);
    }

    private void OnInit(EntityUid uid, CentComConsoleComponent component, ComponentInit args)
    {
        UpdateStation(uid, component);
    }

    private void UpdateStation(EntityUid uid, CentComConsoleComponent component)
    {
        var stationUid = _station.GetOwningStation(uid);
        if (stationUid == null)
            return;
        if (!TryComp<CentCommStationComponent>(stationUid, out var centCommComponent))
            return;
        var meta = MetaData(stationUid.Value);
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
            // Uid = GetNetEntity(uid),
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

        var idName = string.Empty;
        var idPresent = false;
        if (component.IdSlot.Item is { Valid: true } item)
        {
            idName = EntityManager.GetComponent<MetaDataComponent>(item).EntityName;
            idPresent = true;
        }
        var newState = new CentComConsoleBoundUserInterfaceState(idPresent, idName, component.Station);

        _userInterface.SetUiState(uid, CentComConsoleUiKey.Key, newState);
    }
}
