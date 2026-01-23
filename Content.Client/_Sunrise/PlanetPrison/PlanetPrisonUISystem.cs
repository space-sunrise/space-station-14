using System.Linq;
using Content.Client.UserInterface.Systems.Ghost.Controls.PlanetPrison;
using Content.Shared._Sunrise.PlanetPrison;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.PlanetPrison;

public sealed class PlanetPrisonUISystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    private PlanetPrisonWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        // Создаем окно сразу при инициализации системы
        _window = new PlanetPrisonWindow();
        _window.MapsTabPressed += OnMapsTabPressed;
        _window.RolesTabPressed += OnRolesTabPressed;

        PopulateMaps();
        PopulateRoles();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_window != null)
        {
            _window.MapsTabPressed -= OnMapsTabPressed;
            _window.RolesTabPressed -= OnRolesTabPressed;
            _window.Dispose();
            _window = null;
        }
    }

    public void OpenWindow()
    {
        Logger.Info("PlanetPrisonUISystem: OpenWindow called");
        if (_window == null)
        {
            Logger.Error("PlanetPrisonUISystem: Window is null!");
            return;
        }

        // Обновляем данные перед открытием окна
        PopulateMaps();
        PopulateRoles();

        _window.OpenCentered();
        Logger.Info("PlanetPrisonUISystem: Window opened");
    }

    private void OnMapsTabPressed()
    {
        // Обработка переключения на вкладку карт
        PopulateMaps();
    }

    private void OnRolesTabPressed()
    {
        // Обработка переключения на вкладку ролей
        PopulateRoles();
    }

    private void PopulateMaps()
    {
        if (_window == null)
            return;

        _window.ClearMaps();

        // Сначала пытаемся получить данные из компонента, если он существует
        var query = AllEntityQuery<PlanetPrisonSharedComponent>();
        var found = false;
        while (query.MoveNext(out var component))
        {
            found = true;
            Logger.Info($"Found PlanetPrisonSharedComponent with {component.StationsModern.Count} modern maps and {component.StationsOld.Count} old maps");

            // Добавляем современные карты
            foreach (var mapId in component.StationsModern)
            {
                if (_protoManager.TryIndex(mapId, out var mapProto))
                {
                    var entry = new PlanetPrisonMapEntry(
                        Loc.GetString("planet-prison-map-modern", ("name", mapProto.MapName)),
                        Loc.GetString("planet-prison-map-modern-description")
                    );
                    _window.AddMapEntry(entry);
                    Logger.Info($"Added modern map entry: {mapProto.MapName}");
                }
            }

            // Добавляем старые карты
            foreach (var mapId in component.StationsOld)
            {
                if (_protoManager.TryIndex(mapId, out var mapProto))
                {
                    var entry = new PlanetPrisonMapEntry(
                        Loc.GetString("planet-prison-map-old", ("name", mapProto.MapName)),
                        Loc.GetString("planet-prison-map-old-description")
                    );
                    _window.AddMapEntry(entry);
                    Logger.Info($"Added old map entry: {mapProto.MapName}");
                }
            }

            break;
        }

        if (!found)
        {
            Logger.Info("No PlanetPrisonSharedComponent found - using fallback data");

            // Fallback: добавляем примеры карт на основе прототипов
            // Ищем все GameMapPrototype с ID содержащими "PlanetPrison"
            foreach (var proto in _protoManager.EnumeratePrototypes<GameMapPrototype>())
            {
                if (proto.ID.Contains("PlanetPrison", StringComparison.OrdinalIgnoreCase))
                {
                    var entry = new PlanetPrisonMapEntry(
                        proto.MapName,
                        Loc.GetString("planet-prison-map-fallback-description", ("name", proto.MapName))
                    );
                    _window.AddMapEntry(entry);
                    Logger.Info($"Added fallback map entry: {proto.MapName}");
                }
            }

            // Если не нашли ни одной карты, добавляем заглушку
            if (_window.MapsEntryCount == 0)
            {
                var entry = new PlanetPrisonMapEntry(
                    Loc.GetString("planet-prison-map-placeholder-title"),
                    Loc.GetString("planet-prison-map-placeholder-description")
                );
                _window.AddMapEntry(entry);
            }
        }
    }

    private void PopulateRoles()
    {
        if (_window == null)
            return;

        _window.ClearRoles();

        // Пока что добавим заглушку - роли будут заполняться из PlanetPrisonStationComponent или отдельной системы
        var entry = new PlanetPrisonRoleEntry(
            Loc.GetString("planet-prison-role-placeholder-title"),
            Loc.GetString("planet-prison-role-placeholder-description")
        );
        _window.AddRoleEntry(entry);
    }
}
