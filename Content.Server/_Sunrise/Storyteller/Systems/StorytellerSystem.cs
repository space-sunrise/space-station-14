using System.Linq;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.StationEvents;
using Content.Server.StationEvents.Components;
using Content.Server._Sunrise.Storyteller.Components;
using Content.Shared._Sunrise.Storyteller.Prototypes;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Research.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Atmos.Components;
using Content.Server.Ghost.Roles;
using Content.Server.Power.Components;
using Content.Server.Tesla.Components;
using Content.Shared.Atmos;
using Content.Shared.Singularity.Components;
using Content.Shared.Starlight.Energy.Supermatter;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Anomaly.Components;
using Content.Shared.Fluids.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Damage.Components;
using Content.Shared.Tag;
using Content.Shared.Xenoarchaeology.Artifact.Components;


namespace Content.Server._Sunrise.Storyteller.Systems;

/// <summary>
/// The core EntitySystem managing the Sunrise Storyteller (Game Director).
/// Computes crew stress, manages pacing states, and triggers balanced events.
/// </summary>
public sealed partial class StorytellerSystem : GameRuleSystem<StorytellerRuleComponent>
{
    private const string SecurityDepartmentId = "Security";

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly SharedJobSystem _jobSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly EventManagerSystem _eventManager = default!;
    [Dependency] private readonly SharedCargoSystem _cargoSystem = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRoleSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedBatterySystem _batterySystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private readonly List<TimeSpan> _joinTimestamps = new();
    private readonly List<TimeSpan> _leaveTimestamps = new();

    public override void Initialize()
    {
        base.Initialize();

        InitializeMetrics();
        InitializeAI();

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    protected override void Added(EntityUid uid, StorytellerRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        component.NextCheckTime = Timing.CurTime + TimeSpan.FromSeconds(10);

        component.StateTransitionTime = Timing.CurTime + TimeSpan.FromMinutes(_random.Next(15, 30));
        component.PacingState = StorytellerPacingState.BuildUp;
        component.ThreatBudget = 30f;


        var lobbySystem = EntityManager.System<StorytellerLobbySystem>();
        component.StorytellerType = lobbySystem.StorytellerType;
    }

    protected override void Started(EntityUid uid, StorytellerRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
        LogStorytellerState(component, null);
    }

    protected override void ActiveTick(EntityUid uid, StorytellerRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        if (!_cfg.GetCVar(SunriseCCVars.StorytellerEnabled))
            return;


        var budgetModifier = 1f;
        var maxBudgetModifier = 1f;
        if (_protoManager.TryIndex<StorytellerTypePrototype>(component.StorytellerType.ToString(), out var typeProto))
        {
            budgetModifier = typeProto.BudgetModifier;
            maxBudgetModifier = typeProto.MaxBudgetModifier;
        }

        var baseInc = component.BaseBudgetPerSecond * frameTime * budgetModifier;
        var maxBudget = component.MaxThreatBudget * maxBudgetModifier;

        // Stress dampens threat budget generation (higher stress -> crew needs relief -> slow down threat budget)
        var stressModifier = MathF.Max(0.1f, 1f - (component.CrewStress / 80f));
        component.ThreatBudget = MathF.Min(maxBudget, component.ThreatBudget + baseInc * stressModifier);

        // Check pacing state transitions
        if (Timing.CurTime >= component.StateTransitionTime)
        {
            TransitionPacingState(uid, component);
        }

        // Periodic evaluation
        if (Timing.CurTime >= component.NextCheckTime)
        {
            var interval = _cfg.GetCVar(SunriseCCVars.StorytellerCheckInterval);
            component.NextCheckTime = Timing.CurTime + TimeSpan.FromSeconds(interval);
            EvaluateStoryteller((uid, component));
        }
    }

    private void TransitionPacingState(EntityUid uid, StorytellerRuleComponent comp)
    {
        var oldState = comp.PacingState;

        var durationMult = 1f;
        var relMin = 20f;
        var relMax = 40f;
        var bldMin = 15f;
        var bldMax = 30f;
        var pkMin = 12f;
        var pkMax = 24f;
        var recMin = 10f;
        var recMax = 20f;

        if (_protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out var typeProto))
        {
            durationMult = typeProto.DurationMultiplier;
            relMin = typeProto.RelaxationMinMinutes;
            relMax = typeProto.RelaxationMaxMinutes;
            bldMin = typeProto.BuildUpMinMinutes;
            bldMax = typeProto.BuildUpMaxMinutes;
            pkMin = typeProto.PeakMinMinutes;
            pkMax = typeProto.PeakMaxMinutes;
            recMin = typeProto.RecoveryMinMinutes;
            recMax = typeProto.RecoveryMaxMinutes;
        }

        switch (comp.PacingState)
        {
            case StorytellerPacingState.Relaxation:
                comp.PacingState = StorytellerPacingState.BuildUp;
                comp.StateTransitionTime = Timing.CurTime + TimeSpan.FromMinutes(_random.NextFloat(relMin, relMax) * durationMult);
                break;
            case StorytellerPacingState.BuildUp:
                comp.PacingState = StorytellerPacingState.Peak;
                comp.StateTransitionTime = Timing.CurTime + TimeSpan.FromMinutes(_random.NextFloat(bldMin, bldMax) * durationMult);
                break;
            case StorytellerPacingState.Peak:
                comp.PacingState = StorytellerPacingState.Recovery;
                comp.StateTransitionTime = Timing.CurTime + TimeSpan.FromMinutes(_random.NextFloat(pkMin, pkMax) * durationMult);
                break;
            case StorytellerPacingState.Recovery:
                comp.PacingState = StorytellerPacingState.Relaxation;
                comp.StateTransitionTime = Timing.CurTime + TimeSpan.FromMinutes(_random.NextFloat(recMin, recMax) * durationMult);
                break;
        }

        LogStorytellerState(comp, oldState);
    }

    private void EvaluateStoryteller(Entity<StorytellerRuleComponent> entity)
    {
        if (_roundEnd.IsRoundEndRequested())
            return;

        var metrics = CalculateStationMetrics(entity.Comp);
        entity.Comp.CrewStress = CalculateCrewStress(ref metrics);

        // Update Prometheus gauges
        UpdatePrometheusGauges(entity.Comp, metrics);

        // Loki structured logs
        LogTelemetryTick(entity.Comp, metrics);

        // AI storyteller branch
        if (_cfg.GetCVar(SunriseCCVars.StorytellerAiEnabled) && !string.IsNullOrWhiteSpace(_cfg.GetCVar(SunriseCCVars.StorytellerAiUrl)))
        {
            RequestAiEventRecommendation(entity, metrics);
            return;
        }

        // Default heuristic branch
        ExecuteHeuristicStoryteller(entity, metrics);
    }

    private void ExecuteHeuristicStoryteller(Entity<StorytellerRuleComponent> entity, StationMetrics metrics)
    {
        if (!_cfg.GetCVar(CCVars.EventsEnabled))
            return;

        // Pacing & budget check
        var rollChance = 0f;
        switch (entity.Comp.PacingState)
        {
            case StorytellerPacingState.Relaxation:
                rollChance = 0.15f; // low pop/relaxing events
                break;
            case StorytellerPacingState.BuildUp:
                rollChance = 0.35f; // building events
                break;
            case StorytellerPacingState.Peak:
                rollChance = 0.05f; // clamp events during peak
                break;
            case StorytellerPacingState.Recovery:
                // Recovery is meant for quiet breathing room unless crew is severely stressed, in which case we trigger Helpful events
                rollChance = entity.Comp.CrewStress > 40f ? 0.40f : 0.05f;
                break;
        }

        if (!_random.Prob(rollChance))
            return;

        // Find candidate events
        var eligibleEvents = GetEligibleHeuristicEvents(entity.Comp, metrics);
        if (eligibleEvents.Count == 0)
            return;

        // Weighted random pick
        var selected = PickEventFromEligible(eligibleEvents, entity.Comp.ThreatBudget, metrics.StationStrength, entity.Comp.StorytellerType);
        if (selected == null)
            return;

        TriggerEvent(entity, selected.Value.Item1, selected.Value.Item2);
    }

    public StationMetrics CalculateStationMetrics(StorytellerRuleComponent? comp = null)
    {
        var totalPlayers = 0;
        var aliveCount = 0;
        var deadCount = 0;
        var ghostCount = 0;
        var securityCount = 0;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.Status != Robust.Shared.Enums.SessionStatus.InGame)
                continue;

            totalPlayers++;

            if (session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
            {
                ghostCount++;
                continue;
            }

            if (HasComp<GhostComponent>(entity))
            {
                ghostCount++;
                continue;
            }

            if (TryComp<MobStateComponent>(entity, out var mobState))
            {
                if (mobState.CurrentState == MobState.Dead)
                    deadCount++;
                else if (mobState.CurrentState == MobState.Alive || mobState.CurrentState == MobState.Critical)
                {
                    aliveCount++;

                    if (_mindSystem.TryGetMind(entity, out var mindId, out _) && _jobSystem.MindTryGetJob(mindId, out var jobPrototype))
                    {
                        if (IsSecurityJob(jobPrototype))
                        {
                            securityCount++;
                        }
                    }
                }
            }
            else
            {
                aliveCount++;
            }
        }

        var cargoBalance = 0;
        var bankQuery = EntityQueryEnumerator<StationBankAccountComponent>();
        while (bankQuery.MoveNext(out var bankUid, out var bankComp))
        {
            var accounts = _cargoSystem.GetAccounts((bankUid, bankComp));
            if (accounts.TryGetValue(bankComp.PrimaryAccount, out var balance))
            {
                cargoBalance += balance;
            }
            else
            {
                cargoBalance += accounts.Values.Sum();
            }
        }

        var sciencePoints = 0;
        var serverQuery = EntityQueryEnumerator<ResearchServerComponent>();
        while (serverQuery.MoveNext(out _, out var serverComp))
        {
            sciencePoints += serverComp.Points;
        }

        // Calculate expanded storyteller metrics
        CalculateGridAtmosMetrics(out var breachRatio, out var dangerousRatio, out var totalTiles);
        CalculatePowerGridMetrics(out var powerDeficit);
        var weaponsCount = CountCrewWeapons();
        CountAntagAndErt(out var antagCount, out var ertCount);
        GetSingularityTeslaStatus(out var singActive, out var singCont, out var tesActive, out var tesCont);
        var smIntegrity = GetSupermatterIntegrity(out var smActive);
        var researchTiers = GetUnlockedResearchCount();
        var distribution = GetCrewDistribution();
        GetPlayerFlowRates(out var joinRate, out var leaveRate);
        var ghostRolesCount = _ghostRoleSystem.GetGhostRoleCount();


        var anomaliesCount = 0;
        var anomalyQuery = EntityQueryEnumerator<AnomalyComponent, TransformComponent>();
        while (anomalyQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == null || _stationSystem.GetOwningStation(xform.GridUid.Value) == null)
                continue;

            anomaliesCount++;
        }

        var activeArtifactsCount = 0;
        var artifactQuery = EntityQueryEnumerator<XenoArtifactComponent, TransformComponent>();
        while (artifactQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == null || _stationSystem.GetOwningStation(xform.GridUid.Value) == null)
                continue;

            activeArtifactsCount++;
        }

        var puddlesCount = 0;
        var puddleQuery = EntityQueryEnumerator<PuddleComponent, TransformComponent>();
        while (puddleQuery.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid == null || _stationSystem.GetOwningStation(xform.GridUid.Value) == null)
                continue;

            puddlesCount++;
        }

        var trashCount = 0;
        var tagQuery = EntityQueryEnumerator<TagComponent, TransformComponent>();
        while (tagQuery.MoveNext(out var uid, out var tag, out var xform))
        {
            if (tag.Tags.Contains("Trash") && xform.GridUid != null && xform.ParentUid == xform.GridUid)
            {
                if (_stationSystem.GetOwningStation(xform.GridUid.Value) == null)
                    continue;

                trashCount++;
            }
        }

        var totalCrewDamage = 0f;
        var crewWithMindCount = 0;
        foreach (var session in _playerManager.Sessions)
        {
            if (session.Status != Robust.Shared.Enums.SessionStatus.InGame)
                continue;

            if (session.AttachedEntity is not { Valid: true } entityUid || Deleted(entityUid))
                continue;

            if (TryComp<MobStateComponent>(entityUid, out var mobState) &&
                (mobState.CurrentState == MobState.Alive || mobState.CurrentState == MobState.Critical))
            {
                if (TryComp<DamageableComponent>(entityUid, out var damageable))
                {
                    totalCrewDamage += damageable.TotalDamage.Float();
                    crewWithMindCount++;
                }
            }
        }
        var averageCrewDamage = crewWithMindCount > 0 ? totalCrewDamage / crewWithMindCount : 0f;

        return new StationMetrics
        {
            TotalPlayers = totalPlayers,
            AliveCount = aliveCount,
            DeadCount = deadCount,
            GhostCount = ghostCount,
            SecurityCount = securityCount,
            CargoBalance = cargoBalance,
            SciencePoints = sciencePoints,

            AtmosphereBreachRatio = breachRatio,
            DangerousGasesRatio = dangerousRatio,
            PowerGridDeficitRatio = powerDeficit,
            CrewWeaponCount = weaponsCount,
            ActiveAntagonistCount = antagCount,
            ActiveErtCount = ertCount,
            SingularityActive = singActive,
            SingularityContained = singCont,
            TeslaActive = tesActive,
            TeslaContained = tesCont,
            SupermatterIntegrity = smIntegrity,
            SupermatterActive = smActive,
            UnlockedResearchTiers = researchTiers,
            CrewDistribution = distribution,
            PlayerJoinRate = joinRate,
            PlayerLeaveRate = leaveRate,
            AvailableGhostRoles = ghostRolesCount,


            AnomaliesCount = anomaliesCount,
            ActiveArtifactsCount = activeArtifactsCount,
            PuddlesCount = puddlesCount,
            TrashCount = trashCount,
            AverageCrewDamage = averageCrewDamage,
            TotalStationTiles = totalTiles,

            StrengthArmedCrew = CountArmedCrewNotAntags() * (comp != null && _protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out var typeProto) ? typeProto.StrengthArmedCrewCoefficient : 10f),
            StrengthSecurity = securityCount * (comp != null && _protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out typeProto) ? typeProto.StrengthSecurityCoefficient : 15f),
            StrengthCargo = cargoBalance * (comp != null && _protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out typeProto) ? typeProto.StrengthCargoCoefficient : 0.0005f),
            StrengthTechnology = researchTiers * (comp != null && _protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out typeProto) ? typeProto.StrengthTechnologyCoefficient : 3.0f),
            StationStrength = (CountArmedCrewNotAntags() * (comp != null && _protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out typeProto) ? typeProto.StrengthArmedCrewCoefficient : 10f)) +
                              (securityCount * (comp != null && _protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out typeProto) ? typeProto.StrengthSecurityCoefficient : 15f)) +
                              (cargoBalance * (comp != null && _protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out typeProto) ? typeProto.StrengthCargoCoefficient : 0.0005f)) +
                              (researchTiers * (comp != null && _protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out typeProto) ? typeProto.StrengthTechnologyCoefficient : 3.0f))
        };
    }

    public float CalculateCrewStress(ref StationMetrics metrics)
    {
        if (metrics.TotalPlayers == 0)
            return 0f;

        // 1. Dead/Ghost ratio (up to 35 points of stress)
        float deadGhostRatio = (float)(metrics.DeadCount + metrics.GhostCount) / metrics.TotalPlayers;
        float deadStress = Math.Clamp(deadGhostRatio * 35f, 0f, 35f);


        // 2. Singularity/Tesla/Supermatter escape/integrity state (up to 10 points of stress)
        float containmentStress = 0f;
        if (metrics.SingularityActive)
        {
            containmentStress += metrics.SingularityContained ? 2f : 10f;
        }
        if (metrics.TeslaActive)
        {
            containmentStress += metrics.TeslaContained ? 2f : 10f;
        }
        if (metrics.SupermatterActive)
        {
            containmentStress += 3f + ((100f - metrics.SupermatterIntegrity) / 100f * 2f);
        }
        containmentStress = Math.Clamp(containmentStress, 0f, 10f);


        // 3. Security force deficit (up to 10 points of stress)
        float targetSecurity = metrics.TotalPlayers > 5 ? MathF.Max(1f, metrics.TotalPlayers * 0.1f) : 0f;
        float securityStress = 0f;
        if (targetSecurity > 0)
        {
            float deficit = MathF.Max(0f, targetSecurity - metrics.SecurityCount);
            securityStress = Math.Clamp((deficit / targetSecurity) * 10f, 0f, 10f);
        }

        // 4. Economy Distress (up to 5 points of stress) - Sunrise edit - removed RnD points check, only count Cargo deficit
        float economyStress = 0f;
        if (metrics.CargoBalance < 1000) economyStress += 5f;

        // 5. Living Crew Damage (up to 5 points of stress)
        float damageStress = Math.Clamp(metrics.AverageCrewDamage * 0.1f, 0f, 5f);

        // 6. Anomalies & Uncontained active artifacts (up to 6 points of stress)
        float anomalyStress = Math.Clamp((metrics.AnomaliesCount * 2f) + (metrics.ActiveArtifactsCount * 1.5f), 0f, 6f);

        // 7. Station Dirt & Mess (up to 4 points of stress)
        float totalStationTiles = MathF.Max(100f, metrics.TotalStationTiles);
        float puddleDensity = metrics.PuddlesCount / totalStationTiles;
        float trashDensity = metrics.TrashCount / totalStationTiles;
        // Standard density threshold: 1 puddle per 330 tiles (~0.003), 1 trash per 160 tiles (~0.006)
        float messStress = Math.Clamp((puddleDensity / 0.003f) * 2f + (trashDensity / 0.006f) * 2f, 0f, 4f);

        // 8. Power grid deficit (up to 10 points of stress) - Sunrise edit
        float powerStress = Math.Clamp(metrics.PowerGridDeficitRatio * 10f, 0f, 10f);

        // 9. Atmosphere breach / Spaced tiles (up to 10 points of stress) - Sunrise edit
        float breachStress = Math.Clamp(metrics.AtmosphereBreachRatio * 50f, 0f, 10f);

        // 10. Dangerous gases / Toxic air (up to 10 points of stress)
        float gasStress = Math.Clamp(metrics.DangerousGasesRatio * 100f, 0f, 10f);


        metrics.StressDead = deadStress;
        metrics.StressContainment = containmentStress;
        metrics.StressSecurity = securityStress;
        metrics.StressEconomy = economyStress;
        metrics.StressDamage = damageStress;
        metrics.StressAnomaly = anomalyStress;
        metrics.StressMess = messStress;
        metrics.StressPower = powerStress;
        metrics.StressBreach = breachStress;
        metrics.StressGas = gasStress;


        float totalStress = deadStress + containmentStress + securityStress + economyStress + damageStress + anomalyStress + messStress + powerStress + breachStress + gasStress;
        return Math.Clamp(totalStress, 0f, 100f);
    }

    private bool IsSecurityJob(JobPrototype job)
    {
        if (!_jobSystem.TryGetAllDepartments(job.ID, out var departments))
            return false;

        foreach (var dept in departments)
        {
            if (dept.ID == SecurityDepartmentId)
                return true;
        }

        return false;
    }

    private Dictionary<EntityPrototype, StorytellerMetadataPrototype> GetEligibleHeuristicEvents(StorytellerRuleComponent comp, StationMetrics metrics)
    {
        var result = new Dictionary<EntityPrototype, StorytellerMetadataPrototype>();
        var currentDuration = GameTicker.RoundDuration();

        // Query all rule prototypes with storyteller metadata
        foreach (var proto in _protoManager.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.Abstract)
                continue;

            if (!_protoManager.TryIndex<StorytellerMetadataPrototype>(proto.ID, out var metadata))
                continue;

            if (comp.CrewStress < metadata.MinStress || comp.CrewStress > metadata.MaxStress)
                continue;

            // Evac/roundend checks
            if (proto.TryGetComponent<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
            {
                if (metrics.TotalPlayers < stationEvent.MinimumPlayers)
                    continue;

                if (currentDuration.TotalMinutes < stationEvent.EarliestStart)
                    continue;

                // Reoccurrence check
                var lastTime = _eventManager.TimeSinceLastEvent(proto);
                if (lastTime != TimeSpan.Zero && currentDuration.TotalMinutes < stationEvent.ReoccurrenceDelay + lastTime.TotalMinutes)
                    continue;

                if (_roundEnd.IsRoundEndRequested() && !stationEvent.OccursDuringRoundEnd)
                    continue;
            }

            // Gamemode/rule-specific filters based on pacing state
            if (comp.PacingState == StorytellerPacingState.Recovery)
            {
                // In Recovery, we ONLY spawn helpful or neutral events
                if (metadata.ThreatType != StorytellerThreatType.Helpful && metadata.ThreatType != StorytellerThreatType.Neutral)
                    continue;
            }
            else if (comp.PacingState == StorytellerPacingState.Relaxation)
            {
                // In Relaxation, we allow helpful, neutral, and minor calm threats
                if (metadata.ThreatType == StorytellerThreatType.MajorCalm || metadata.ThreatType == StorytellerThreatType.MajorAntag)
                    continue;
            }
            else if (comp.PacingState == StorytellerPacingState.Peak)
            {
                // In Peak, we do not spawn major antags (they are already spawned or playing out)
                if (metadata.ThreatType == StorytellerThreatType.MajorAntag)
                    continue;
            }

            var preservationThreshold = 40f;
            if (_protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out var typeProto))
            {
                preservationThreshold = typeProto.BuildUpPreservationThreshold;
            }

            if (comp.PacingState == StorytellerPacingState.BuildUp && comp.ThreatBudget < preservationThreshold)
            {
                if (metadata.ThreatType != StorytellerThreatType.Helpful &&
                    metadata.ThreatType != StorytellerThreatType.Neutral &&
                    metadata.ThreatCost > 0f)
                {
                    continue;
                }
            }


            // Budget check
            if (metadata.ThreatType != StorytellerThreatType.Helpful && metadata.ThreatCost > comp.ThreatBudget)
                continue;

            // Prevent already running rules
            if (GameTicker.IsGameRuleActive(proto.ID))
                continue;

            result.Add(proto, metadata);
        }

        return result;
    }

    private (EntityPrototype, StorytellerMetadataPrototype)? PickEventFromEligible(
        Dictionary<EntityPrototype, StorytellerMetadataPrototype> available,
        float currentBudget,
        float stationStrength,
        StorytellerType storytellerType)
    {
        // Loaded dynamically from storyteller prototype
        var highBudgetThreshold = 40f;
        var majorMult = 8f;
        var minorMult = 0.1f;
        var scalingFactor = 50f;

        if (_protoManager.TryIndex<StorytellerTypePrototype>(storytellerType.ToString(), out var typeProto))
        {
            highBudgetThreshold = typeProto.HighBudgetThreshold;
            majorMult = typeProto.MajorThreatWeightMultiplier;
            minorMult = typeProto.MinorThreatWeightMultiplier;
            scalingFactor = typeProto.StationStrengthScalingFactor;
        }

        var totalWeight = 0f;
        foreach (var (proto, metadata) in available)
        {
            var baseWeight = 10f;
            if (proto.TryGetComponent<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
            {
                baseWeight = stationEvent.Weight;
            }

            var weight = baseWeight * metadata.WeightModifier;


            if (currentBudget >= highBudgetThreshold)
            {
                if (metadata.ThreatType == StorytellerThreatType.MajorAntag ||
                    metadata.ThreatType == StorytellerThreatType.MajorCalm)
                {
                    // Scale up major threats weight based on current budget and station strength
                    var strengthMult = 1f + (stationStrength / scalingFactor);
                    weight *= majorMult * strengthMult; // Strongly prioritize big threats scaled by station power
                }
                else if (metadata.ThreatType == StorytellerThreatType.Neutral ||
                         metadata.ThreatType == StorytellerThreatType.MinorCalm)
                {
                    weight *= minorMult; // Heavily de-prioritize cheap clutter events
                }
            }


            totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return null;

        var roll = _random.NextFloat(totalWeight);
        foreach (var (proto, metadata) in available)
        {
            var baseWeight = 10f;
            if (proto.TryGetComponent<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
            {
                baseWeight = stationEvent.Weight;
            }

            var weight = baseWeight * metadata.WeightModifier;


            if (currentBudget >= highBudgetThreshold)
            {
                if (metadata.ThreatType == StorytellerThreatType.MajorAntag ||
                    metadata.ThreatType == StorytellerThreatType.MajorCalm)
                {
                    var strengthMult = 1f + (stationStrength / scalingFactor);
                    weight *= majorMult * strengthMult;
                }
                else if (metadata.ThreatType == StorytellerThreatType.Neutral ||
                         metadata.ThreatType == StorytellerThreatType.MinorCalm)
                {
                    weight *= minorMult;
                }
            }


            roll -= weight;
            if (roll <= 0f)
            {
                return (proto, metadata);
            }
        }

        var first = available.First();
        return (first.Key, first.Value);
    }

    private void TriggerEvent(Entity<StorytellerRuleComponent> entity, EntityPrototype proto, StorytellerMetadataPrototype metadata)
    {
        // Deduct cost / process rewards
        entity.Comp.ThreatBudget = MathF.Max(0f, entity.Comp.ThreatBudget - metadata.ThreatCost);

        // Spawn and start rule
        var ruleUid = GameTicker.AddGameRule(proto.ID);
        GameTicker.StartGameRule(ruleUid);

        // Record history
        entity.Comp.ActiveStorytellerRules.Add(ruleUid);
        entity.Comp.EventHistory.Add(proto.ID);

        // Metrics & Logging
        RecordEventTriggered(proto.ID, metadata);
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        _joinTimestamps.Add(Timing.CurTime);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == Robust.Shared.Enums.SessionStatus.Disconnected)
        {
            _leaveTimestamps.Add(Timing.CurTime);
        }
    }

    private void GetPlayerFlowRates(out float joinRate, out float leaveRate)
    {
        var cutoff = Timing.CurTime - TimeSpan.FromMinutes(10);
        _joinTimestamps.RemoveAll(t => t < cutoff);
        _leaveTimestamps.RemoveAll(t => t < cutoff);
        joinRate = _joinTimestamps.Count;
        leaveRate = _leaveTimestamps.Count;
    }

    private void CalculateGridAtmosMetrics(out float breachRatio, out float dangerousGasesRatio, out int totalTiles)
    {
        breachRatio = 0f;
        dangerousGasesRatio = 0f;
        totalTiles = 0;

        var spacedTiles = 0;
        var dangerousTiles = 0;

        var query = EntityQueryEnumerator<GridAtmosphereComponent, TransformComponent>();
        while (query.MoveNext(out var gridUid, out var atmos, out var xform))
        {

            if (_stationSystem.GetOwningStation(gridUid) == null)
                continue;

            foreach (var tile in atmos.Tiles.Values)
            {
                if (tile.NoGridTile)
                    continue;

                totalTiles++;
                if (tile.Space || tile.Air == null || tile.MapAtmosphere)
                {
                    spacedTiles++;
                }
                else
                {
                    var air = tile.Air;

                    if (air.GetMoles(Gas.Plasma) > 0.5f ||
                        air.GetMoles(Gas.Tritium) > 0.5f ||
                        air.GetMoles(Gas.Zauker) > 0.1f ||
                        air.GetMoles(Gas.NitrousOxide) > 1.0f ||
                        air.GetMoles(Gas.BZ) > 0.5f ||
                        air.GetMoles(Gas.Nitrium) > 0.5f ||
                        air.GetMoles(Gas.Frezon) > 0.5f ||
                        air.GetMoles(Gas.Hydrogen) > 1.0f ||
                        air.GetMoles(Gas.ChargedElectrovae) > 0.1f ||
                        air.Temperature > 340f ||
                        air.Temperature < 250f)
                    {
                        dangerousTiles++;
                    }
                }
            }
        }

        if (totalTiles > 0)
        {
            breachRatio = (float)spacedTiles / totalTiles;

            dangerousGasesRatio = (float)dangerousTiles / totalTiles;
        }
    }

    private void CalculatePowerGridMetrics(out float powerDeficitRatio)
    {
        var apcQuery = EntityQueryEnumerator<ApcComponent, BatteryComponent, TransformComponent>();
        var totalApcs = 0;
        var deadApcs = 0;
        while (apcQuery.MoveNext(out var uid, out _, out var battery, out var xform))
        {

            if (xform.GridUid == null || _stationSystem.GetOwningStation(xform.GridUid.Value) == null)
                continue;

            totalApcs++;

            if (_batterySystem.GetCharge((uid, battery)) <= battery.MaxCharge * 0.1f)
            {
                deadApcs++;
            }
        }
        powerDeficitRatio = totalApcs > 0 ? (float)deadApcs / totalApcs : 0f;
    }

    private int CountCrewWeapons()
    {

        var armedMobs = new HashSet<EntityUid>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var mobQuery = GetEntityQuery<MobStateComponent>();

        var gunQuery = EntityQueryEnumerator<GunComponent>();
        while (gunQuery.MoveNext(out var uid, out _))
        {
            var mob = FindCarryingMob(uid, xformQuery, mobQuery);
            if (mob != null && mobQuery.TryGetComponent(mob.Value, out var mobState))
            {
                if (mobState.CurrentState == MobState.Alive || mobState.CurrentState == MobState.Critical)
                {
                    armedMobs.Add(mob.Value);
                }
            }
        }

        var meleeQuery = EntityQueryEnumerator<MeleeWeaponComponent>();
        while (meleeQuery.MoveNext(out var uid, out var melee))
        {
            if (melee.Damage.GetTotal().Float() <= 10)
                continue;

            var mob = FindCarryingMob(uid, xformQuery, mobQuery);
            if (mob != null && mobQuery.TryGetComponent(mob.Value, out var mobState))
            {
                if (mobState.CurrentState == MobState.Alive || mobState.CurrentState == MobState.Critical)
                {
                    armedMobs.Add(mob.Value);
                }
            }
        }
        return armedMobs.Count;

    }

    private int CountArmedCrewNotAntags()
    {
        var armedMobs = new HashSet<EntityUid>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var mobQuery = GetEntityQuery<MobStateComponent>();

        var gunQuery = EntityQueryEnumerator<GunComponent>();
        while (gunQuery.MoveNext(out var uid, out _))
        {
            var mob = FindCarryingMob(uid, xformQuery, mobQuery);
            if (mob != null && mobQuery.TryGetComponent(mob.Value, out var mobState))
            {
                if (mobState.CurrentState == MobState.Alive || mobState.CurrentState == MobState.Critical)
                {
                    // Filter out antagonists and security personnel (as they are already counted under security strength)
                    if (_mindSystem.TryGetMind(mob.Value, out var mindId, out _))
                    {
                        if (_roleSystem.MindIsAntagonist(mindId))
                            continue;
                        if (_jobSystem.MindTryGetJob(mindId, out var job) && IsSecurityJob(job))
                            continue;
                    }

                    armedMobs.Add(mob.Value);
                }
            }
        }

        var meleeQuery = EntityQueryEnumerator<MeleeWeaponComponent>();
        while (meleeQuery.MoveNext(out var uid, out var melee))
        {
            if (melee.Damage.GetTotal().Float() <= 10)
                continue;

            var mob = FindCarryingMob(uid, xformQuery, mobQuery);
            if (mob != null && mobQuery.TryGetComponent(mob.Value, out var mobState))
            {
                if (mobState.CurrentState == MobState.Alive || mobState.CurrentState == MobState.Critical)
                {
                    // Filter out antagonists and security personnel (as they are already counted under security strength)
                    if (_mindSystem.TryGetMind(mob.Value, out var mindId, out _))
                    {
                        if (_roleSystem.MindIsAntagonist(mindId))
                            continue;
                        if (_jobSystem.MindTryGetJob(mindId, out var job) && IsSecurityJob(job))
                            continue;
                    }

                    armedMobs.Add(mob.Value);
                }
            }
        }
        return armedMobs.Count;
    }

    private EntityUid? FindCarryingMob(EntityUid uid, EntityQuery<TransformComponent> xformQuery, EntityQuery<MobStateComponent> mobQuery)
    {
        var current = uid;
        while (true)
        {
            if (!xformQuery.TryGetComponent(current, out var xform))
                break;

            if (!xform.ParentUid.Valid)
                break;

            if (mobQuery.HasComponent(xform.ParentUid))
                return xform.ParentUid;

            current = xform.ParentUid;
        }
        return null;
    }

    private void CountAntagAndErt(out int antagCount, out int ertCount)
    {
        antagCount = 0;
        ertCount = 0;

        foreach (var session in _playerManager.Sessions)
        {
            if (session.Status != Robust.Shared.Enums.SessionStatus.InGame)
                continue;

            if (session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                continue;

            if (TryComp<MobStateComponent>(entity, out var mobState) && mobState.CurrentState == MobState.Dead)
                continue;

            if (_mindSystem.TryGetMind(entity, out var mindId, out _))
            {
                if (_roleSystem.MindIsAntagonist(mindId))
                {
                    antagCount++;
                }

                if (_jobSystem.MindTryGetJob(mindId, out var job))
                {
                    if (_jobSystem.TryGetAllDepartments(job.ID, out var departments))
                    {
                        foreach (var dept in departments)
                        {
                            if (dept.ID == "SpecialOperations" || dept.ID == "TSF" || dept.ID == "CentralCommand")
                            {
                                ertCount++;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private void GetSingularityTeslaStatus(out bool singuloActive, out bool singuloContained, out bool teslaActive, out bool teslaContained)
    {
        singuloActive = false;
        singuloContained = true;
        teslaActive = false;
        teslaContained = true;

        var fieldsExist = false;
        var fieldQuery = EntityQueryEnumerator<ContainmentFieldComponent>();
        if (fieldQuery.MoveNext(out _, out _))
        {
            fieldsExist = true;
        }

        var singuloQuery = EntityQueryEnumerator<SingularityComponent, TransformComponent>();
        while (singuloQuery.MoveNext(out _, out _, out var xform))
        {
            singuloActive = true;
            if (!fieldsExist || !IsNearActiveContainmentField(xform))
            {
                singuloContained = false;
            }
        }

        var teslaQuery = EntityQueryEnumerator<TeslaEnergyBallComponent, TransformComponent>();
        while (teslaQuery.MoveNext(out _, out _, out var xform))
        {
            teslaActive = true;
            if (!fieldsExist || !IsNearActiveContainmentField(xform))
            {
                teslaContained = false;
            }
        }
    }

    private bool IsNearActiveContainmentField(TransformComponent entityXform)
    {
        var fieldQuery = EntityQueryEnumerator<ContainmentFieldComponent, TransformComponent>();
        while (fieldQuery.MoveNext(out _, out _, out var fieldXform))
        {
            if (entityXform.MapID == fieldXform.MapID)
            {
                var distance = (entityXform.WorldPosition - fieldXform.WorldPosition).Length();
                if (distance <= 10.0f) // Within 10 meters of any active containment field
                {
                    return true;
                }
            }
        }
        return false;
    }

    private float GetSupermatterIntegrity(out bool active)
    {
        var minIntegrity = 100f;
        var smQuery = EntityQueryEnumerator<SupermatterComponent, TransformComponent>();
        var anySm = false;
        while (smQuery.MoveNext(out _, out var sm, out var xform))
        {
            if (xform.GridUid == null || _stationSystem.GetOwningStation(xform.GridUid.Value) == null)
                continue;

            anySm = true;
            if (sm.Durability.Float() < minIntegrity)
            {
                minIntegrity = sm.Durability.Float();
            }
        }
        active = anySm;
        return anySm ? minIntegrity : 100f;
    }

    private int GetUnlockedResearchCount()
    {

        var uniqueTechs = new HashSet<string>();
        var techQuery = EntityQueryEnumerator<TechnologyDatabaseComponent>();
        while (techQuery.MoveNext(out _, out var techDb))
        {
            foreach (var techId in techDb.UnlockedTechnologies)
            {
                uniqueTechs.Add(techId.Id);
            }
        }

        var totalScore = 0f;
        foreach (var techId in uniqueTechs)
        {
            if (!_protoManager.TryIndex<Content.Shared.Research.Prototypes.TechnologyPrototype>(techId, out var techProto))
                continue;

            // Tier weight: T3 is much more important than T1
            var tierWeight = techProto.Tier switch
            {
                1 => 1f,
                2 => 3f,
                3 => 8f,
                _ => 1f
            };

            // Discipline weight: loaded dynamically from the discipline prototype to avoid hardcoding IDs
            var disciplineMult = 1.0f;
            if (_protoManager.TryIndex(techProto.Discipline, out var disciplineProto))
            {
                disciplineMult = disciplineProto.StorytellerUsefulness;
            }

            totalScore += tierWeight * disciplineMult;
        }

        return (int)MathF.Round(totalScore);

    }

    private Dictionary<string, int> GetCrewDistribution()
    {
        var dist = new Dictionary<string, int>();
        foreach (var dept in _protoManager.EnumeratePrototypes<DepartmentPrototype>())
        {
            dist[dept.ID] = 0;
        }

        foreach (var session in _playerManager.Sessions)
        {
            if (session.Status != Robust.Shared.Enums.SessionStatus.InGame)
                continue;

            if (session.AttachedEntity is not { Valid: true } entity || Deleted(entity))
                continue;

            if (TryComp<MobStateComponent>(entity, out var mobState) && mobState.CurrentState == MobState.Dead)
                continue;

            if (_mindSystem.TryGetMind(entity, out var mindId, out _) && _jobSystem.MindTryGetJob(mindId, out var job))
            {
                if (_jobSystem.TryGetAllDepartments(job.ID, out var departments))
                {
                    foreach (var dept in departments)
                    {
                        if (dist.ContainsKey(dept.ID))
                        {
                            dist[dept.ID]++;
                        }
                    }
                }
            }
        }
        return dist;
    }
}

public struct StationMetrics
{
    public int TotalPlayers;
    public int AliveCount;
    public int DeadCount;
    public int GhostCount;
    public int SecurityCount;
    public int CargoBalance;
    public int SciencePoints;
    public float AtmosphereBreachRatio;
    public float DangerousGasesRatio;
    public float PowerGridDeficitRatio;
    public int CrewWeaponCount;
    public int ActiveAntagonistCount;
    public int ActiveErtCount;
    public bool SingularityActive;
    public bool SingularityContained;
    public bool TeslaActive;
    public bool TeslaContained;
    public float SupermatterIntegrity;
    public bool SupermatterActive;
    public int UnlockedResearchTiers;
    public Dictionary<string, int> CrewDistribution;
    public float PlayerJoinRate;
    public float PlayerLeaveRate;
    public int AvailableGhostRoles;
    public int AnomaliesCount;
    public int ActiveArtifactsCount;
    public int PuddlesCount;
    public int TrashCount;
    public float AverageCrewDamage;
    public int TotalStationTiles;
    public float StationStrength;
    public float StressDead;
    public float StressContainment;
    public float StressSecurity;
    public float StressEconomy;
    public float StressDamage;
    public float StressAnomaly;
    public float StressMess;
    public float StrengthArmedCrew;
    public float StrengthSecurity;
    public float StrengthCargo;
    public float StrengthTechnology;
    public float StressPower;
    public float StressBreach;
    public float StressGas;
}
