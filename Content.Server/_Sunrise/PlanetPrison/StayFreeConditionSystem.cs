using Content.Server.GameTicking;
using Content.Server.RoundEnd;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.PlanetPrison;

public sealed class StayFreeConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

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

        bool isRestrained = false;
        if (TryComp<CuffableComponent>(entity, out var cuffed))
        {
            if (!cuffed.CanStillInteract)
                isRestrained = true;

            if (!isRestrained && TryComp<HandsComponent>(entity, out var hands) && cuffed.CuffedHandCount >= hands.Count)
                isRestrained = true;
        }

        if (isRestrained)
            return 0.1f;

        if (!(_emergencyShuttle.EmergencyShuttleArrived ||
              _roundEnd.IsRoundEndRequested() ||
              _gameTicker.RunLevel != GameRunLevel.InRound))
            return 0.5f;

        return 1f;
    }

    private void OnCuffedStateChanged(EntityUid uid, CuffableComponent component, ref CuffedStateChangeEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        if (!_mind.TryFindObjective((mindId, mind), StayFreeObjective, out var objective))
            return;

        var progressEv = new ObjectiveGetProgressEvent(mindId, mind);
        RaiseLocalEvent(objective.Value, ref progressEv);
    }
}

