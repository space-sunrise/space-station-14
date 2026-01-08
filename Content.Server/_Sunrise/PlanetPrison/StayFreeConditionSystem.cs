using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.PlanetPrison;

public sealed class StayFreeConditionSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private readonly EntProtoId _stayFreeObjective = "PlanetPrisonerStayFreeObjective";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StayFreeConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<CuffableComponent, CuffedStateChangeEvent>(OnCuffedStateChanged);
    }

    private void OnGetProgress(Entity<StayFreeConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.MindId, args.Mind);
    }

    /// <summary>
    /// Проверяет, скован ли игрок (в наручниках, смирительной рубашке и т.п.)
    /// Игрок считается скованным, если:
    /// 1. Не может взаимодействовать руками (CanStillInteract == false)
    /// 2. Все руки закованы (CuffedHandCount >= общее количество рук)
    /// </summary>
    private bool IsRestrained(EntityUid entity)
    {
        if (!TryComp<CuffableComponent>(entity, out var cuffed))
            return false;

        if (!cuffed.CanStillInteract)
            return true;

        if (TryComp<HandsComponent>(entity, out var hands) && cuffed.CuffedHandCount >= hands.Count)
            return true;

        return false;
    }

    private float GetProgress(EntityUid mindId, MindComponent mind)
    {
        if (mind.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
            return 0f;

        var entity = mind.OwnedEntity.Value;
        var isRestrained = IsRestrained(entity);

        // Проверяем, достигнут ли конец раунда. Цель должна проверяться только в конце раунда или при эвакуации.
        // Условия окончания раунда:
        // 1. Прибыл эвакуационный шаттл
        // 2. Запрошено окончание раунда (например, через админ-команду endround)
        // 3. Раунд уже не в состоянии InRound (обрабатывает случай, когда раунд уже завершён)
        var shuttleArrived = _emergencyShuttle.EmergencyShuttleArrived;
        var roundEndRequested = _roundEnd.IsRoundEndRequested();
        var roundNotInProgress = _gameTicker.RunLevel != GameRunLevel.InRound;
        var endReached = shuttleArrived || roundEndRequested || roundNotInProgress;

        // Во время раунда:
        // 1. если жив и не закован - 50%,
        // 2. если жив, но закован - 10%.
        if (!endReached)
        {
            if (isRestrained)
                return 0.1f;
            return 0.5f;
        }

        // В конце раунда:
        // 1. если жив и не закован - 100%,
        // 2. если жив, но закован - 50%.
        if (isRestrained)
            return 0.5f;

        return 1f;
    }

    private void OnCuffedStateChanged(Entity<CuffableComponent> ent, ref CuffedStateChangeEvent args)
    {
        if (!_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            return;

        if (!_mind.TryFindObjective((mindId, mind), _stayFreeObjective, out var objectiveUid))
            return;

        if (!TryComp<StayFreeConditionComponent>(objectiveUid.Value, out var conditionComp))
            return;

        // Критически важно использовать единую логику проверки isRestrained:
        // рассинхронизация между иконкой и прогрессом приведёт к багу, когда игрок видит иконку свободного,
        // но прогресс соответствует закованному (или наоборот).
        var isRestrained = IsRestrained(ent.Owner);

        if (isRestrained)
        {
            // SetIcon перезаписывает исходное значение, поэтому сохраняем оригинал для последующего восстановления
            // при развязывании наручников. Флаг предотвращает повторное сохранение (перезапись может изменить иконку).
            if (!conditionComp.IconOverridden)
            {
                if (TryComp<ObjectiveComponent>(objectiveUid.Value, out var objComp) && objComp.Icon != null)
                    conditionComp.OriginalIcon = objComp.Icon;

                conditionComp.IconOverridden = true;
            }

            _objectives.SetIcon(objectiveUid.Value, conditionComp.RestrainedIcon);
        }
        else
        {
            // SetIcon не откатывается автоматически, поэтому явно восстанавливаем сохранённое значение.
            if (conditionComp.IconOverridden)
            {
                // Если OriginalIcon был сохранён, используем его. Иначе получаем иконку из прототипа цели
                // (защита от случая, когда иконка отсутствовала на момент сохранения).
                var iconToRestore = conditionComp.OriginalIcon;
                if (iconToRestore == null)
                {
                    var objectiveProto = _proto.Index(_stayFreeObjective);
                    if (objectiveProto.TryGetComponent<ObjectiveComponent>(out var protoObjComp, _componentFactory) && protoObjComp.Icon != null)
                        iconToRestore = protoObjComp.Icon;
                }

                if (iconToRestore != null)
                    _objectives.SetIcon(objectiveUid.Value, iconToRestore);
            }

            conditionComp.IconOverridden = false;
        }
    }
}

