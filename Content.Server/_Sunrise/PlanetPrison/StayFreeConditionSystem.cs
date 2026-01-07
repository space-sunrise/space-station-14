using Content.Server.GameTicking;
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
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string StayFreeObjective = "PlanetPrisonerStayFreeObjective";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StayFreeConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<CuffableComponent, CuffedStateChangeEvent>(OnCuffedStateChanged);
    }

    private void OnGetProgress(EntityUid uid, StayFreeConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(args.MindId, args.Mind);
    }

    private float GetProgress(EntityUid mindId, MindComponent mind)
    {
        if (mind.OwnedEntity == null || _mind.IsCharacterDeadIc(mind))
            return 0f;

        var entity = mind.OwnedEntity.Value;

        var isRestrained = false;
        if (TryComp<CuffableComponent>(entity, out var cuffed))
        {
            if (!cuffed.CanStillInteract)
                isRestrained = true;

            if (!isRestrained && TryComp<HandsComponent>(entity, out var hands) && cuffed.CuffedHandCount >= hands.Count)
                isRestrained = true;
        }

        // До конца раунда / эвакуации прогресс не должен считаться завершённым.
        var endReached = _emergencyShuttle.EmergencyShuttleArrived ||
                         _roundEnd.IsRoundEndRequested() ||
                         _gameTicker.RunLevel != GameRunLevel.InRound;

        // Во время раунда: если жив и не закован — 50%, если закован — 10%.
        if (!endReached)
        {
            if (isRestrained)
                return 0.1f;
            return 0.5f;
        }

        // В конце раунда: если жив и свободен — 100%, если жив, но закован — 50%.
        if (isRestrained)
            return 0.5f;

        return 1f;
    }

    private void OnCuffedStateChanged(EntityUid uid, CuffableComponent component, ref CuffedStateChangeEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        if (!_mind.TryFindObjective((mindId, mind), StayFreeObjective, out var objectiveUid))
            return;

        if (!TryComp<StayFreeConditionComponent>(objectiveUid.Value, out var conditionComp))
            return;

        var isRestrained = !component.CanStillInteract;

        if (isRestrained)
        {
            // Сохраняем оригинальную иконку, если ещё не сохранили.
            if (!conditionComp.IconOverridden)
            {
                if (TryComp<ObjectiveComponent>(objectiveUid.Value, out var objComp) && objComp.Icon != null)
                    conditionComp.OriginalIcon = objComp.Icon;

                conditionComp.IconOverridden = true;
            }

            // Используем иконку из прототипа (по умолчанию — иконка алерта Handcuffed).
            _objectives.SetIcon(objectiveUid.Value, conditionComp.RestrainedIcon);
        }
        else
        {
            // Восстанавливаем исходную иконку, если она была переопределена.
            if (conditionComp.IconOverridden && conditionComp.OriginalIcon != null)
                _objectives.SetIcon(objectiveUid.Value, conditionComp.OriginalIcon);

            conditionComp.IconOverridden = false;
        }
    }
}

