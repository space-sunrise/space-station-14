using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Server.Station.Systems;
using Content.Server.AlertLevel;
using Content.Shared.Station.Components;

namespace Content.Server.Access.Systems; //Sunrise-edited

public sealed class AccessSystem : SharedAccessSystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }
    /// <summary>
    /// Запускает обновление уровня аварийных доступов на всех сущностях с AccessReaderComponent
    /// </summary>
    private void OnAlertLevelChanged(AlertLevelChangedEvent ev)
    {

        if (!TryComp<AlertLevelComponent>(ev.Station, out var alert))
            return;

        if (alert.AlertLevels == null)
            return;

        var query = EntityQueryEnumerator<AccessReaderComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var reader, out var xform))
        {

            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != ev.Station)
                continue;

            if (alert.AlertLevels == null)
                return;

            if (!TryComp<AccessReaderComponent>(uid, out var comp))
                return;

            if (comp.AlertAccesses.Count == 0)
                continue;

            Update((uid, reader));
            Dirty(uid, reader);
        }
    }

    /// <summary>
    /// Устанавливает значение из прототипа в зависимости от кода
    /// </summary>
    public void Update(Entity<AccessReaderComponent> entity)
    {

        if (!TryComp<AlertLevelComponent>(_station.GetOwningStation(entity.Owner), out var alerts))
            return;

        if (alerts.AlertLevels == null)
            return;

        var alertLevels = new Dictionary<string, AccessReaderComponent.CurrentAlertLevel>
        {
            { "blue", AccessReaderComponent.CurrentAlertLevel.blue },
            { "red", AccessReaderComponent.CurrentAlertLevel.red },
            { "yellow", AccessReaderComponent.CurrentAlertLevel.yellow },
            { "gamma", AccessReaderComponent.CurrentAlertLevel.gamma },
            { "delta", AccessReaderComponent.CurrentAlertLevel.delta }
        };

        entity.Comp.Group = string.Empty; // Значение по умолчанию
        foreach (var level in alertLevels)
        {
            if (alerts.CurrentLevel.Contains(level.Key))
            {
                if (entity.Comp.AlertAccesses.TryGetValue(level.Value, out var value))
                {
                    entity.Comp.Group = value;
                    break;
                }
            }
        }
    }
}
