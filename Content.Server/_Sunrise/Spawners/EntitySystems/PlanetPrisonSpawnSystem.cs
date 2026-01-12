using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Server.Station.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Map;

namespace Content.Server.Spawners.EntitySystems;

/// <summary>
/// Обрабатывает спавн для ролей Планетарной Тюрьмы, имеющих особое поведение спавна.
/// Эта система запускается перед стандартной SpawnPointSystem, чтобы переопределить
/// логику спавна по умолчанию для этих специфических ролей.
/// </summary>
public sealed class PlanetPrisonSpawnSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;

    private ProtoId<DepartmentPrototype> _planetPrisonDepartmentId = "PlanetPrison";

    public override void Initialize()
    {
        // Подписываемся на событие перед SpawnPointSystem, чтобы наша логика обработки
        // ролей Планетарной Тюрьмы срабатывала первой и могла переопределить стандартное поведение.
        SubscribeLocalEvent<PlayerSpawningEvent>(OnPlayerSpawning, before: new[] { typeof(SpawnPointSystem) });
    }

    private void OnPlayerSpawning(PlayerSpawningEvent args)
    {
        // Если другая система уже обработала спавн, пропускаем.
        if (args.SpawnResult != null)
            return;

        // Проверяем, является ли это ролью Планетарной Тюрьмы.
        if (!IsPlanetPrisonRole(args.Job))
            return;

        // Обрабатываем спавн для роли Планетарной Тюрьмы, находя подходящие позиции.
        var spawnPositions = FindPlanetPrisonSpawnPositions(args);

        // Если подходящих позиций не найдено, выводим предупреждение и позволяем другим системам обработать спавн.
        if (spawnPositions.Count == 0)
        {
            Log.Warning($"Подходящие позиции спавна для роли Планетарной Тюрьмы {args.Job} не найдены.");
            return; // Позволяем другим системам обработать это, так как у нас нет подходящих мест.
        }

        // Выбираем случайную позицию из найденных и спавним игрока.
        var spawnLocation = _random.Pick(spawnPositions);
        args.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLocation,
            args.Job,
            args.HumanoidCharacterProfile,
            args.Station);
    }

    /// <summary>
    /// Проверяет, является ли переданная роль (job) одной из ролей Планетарной Тюрьмы.
    /// Это необходимо для определения, должна ли система PlanetPrisonSpawnSystem
    /// взять на себя управление процессом спавна данного игрока.
    /// </summary>
    private bool IsPlanetPrisonRole(ProtoId<JobPrototype>? jobId)
    {
        if (jobId == null)
            return false;

        if (!_prototypeManager.TryIndex(jobId.Value, out var jobProto))
            return false;

        if (!_jobSystem.TryGetDepartment(jobProto.ID, out var departmentPrototype))
            return false;

        // Проверяем, принадлежит ли работа к департаменту "PlanetPrison".
        return departmentPrototype.ID == _planetPrisonDepartmentId;
    }

    private List<EntityCoordinates> FindPlanetPrisonSpawnPositions(PlayerSpawningEvent args)
    {
        // Сначала пытаемся найти специфические спавнеры Планетарной Тюрьмы для желаемого типа спавна.
        var positions = GetValidPrisonSpawners(args, args.DesiredSpawnPointType);

        // Запасной вариант: если для позднего присоединения (DesiredSpawnPointType == LateJoin)
        // не найдено специфических спавнеров Планетарной Тюрьмы, предназначенных именно для позднего присоединения,
        // то в качестве отката пытаемся найти обычные спавнеры типа "Job" (предназначенные для старта раунда)
        // для этой же роли. Это гарантирует, что игроки Планетарной Тюрьмы всегда смогут заспавниться,
        // даже если специализированные LateJoin-спавнеры отсутствуют.
        if (positions.Count == 0 && args.DesiredSpawnPointType == SpawnPointType.LateJoin)
        {
            positions = GetValidPrisonSpawners(args, SpawnPointType.Job);
        }

        return positions;
    }

    /// <summary>
    /// Получает список действительных точек спавна Планетарной Тюрьмы для заданного типа спавна.
    /// </summary>
    private List<EntityCoordinates> GetValidPrisonSpawners(PlayerSpawningEvent args, SpawnPointType targetSpawnPointType)
    {
        var validPositions = new List<EntityCoordinates>();
        var query = EntityQueryEnumerator<SpawnPointComponent, PlanetPrisonSpawnComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var spawnPoint, out var prisonComp, out var xform))
        {
        // Пропускаем спавнеры, находящиеся не на той станции.
        if (args.Station != null && _stationSystem.GetOwningStation(uid, xform) != args.Station)
            continue;

        // Проверяем, подходит ли этот спавнер для текущей работы и желаемого типа спавна.
        if (IsValidPrisonSpawnerInternal(spawnPoint, prisonComp, args.Job, targetSpawnPointType))
        {
            validPositions.Add(xform.Coordinates);
        }
        }

        return validPositions;
    }

/// <summary>
/// Внутренний вспомогательный метод для проверки того, является ли спавнер Планетарной Тюрьмы действительным.
/// </summary>
private bool IsValidPrisonSpawnerInternal(SpawnPointComponent spawnPoint, PlanetPrisonSpawnComponent prisonComp, ProtoId<JobPrototype>? job, SpawnPointType targetSpawnPointType)
    {
        // Проверяем, поддерживает ли спавнер требуемый тип спавна.
        if (spawnPoint.SpawnType != targetSpawnPointType)
            return false;

        // Дополнительная фильтрация по работе, если спавнер предназначен для конкретной работы.
        if (spawnPoint.Job != null && job != null && spawnPoint.Job != job)
            return false;

        // Проверяем, включает ли предпочтительные типы спавна спавнера целевой тип спавна.
        return prisonComp.PreferredSpawnTypes.Contains(targetSpawnPointType);
    }
}
