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
using Content.Shared.Power.Components; // Sunrise-Edit
using Content.Shared.Power.EntitySystems; // Sunrise-Edit
using Content.Shared.Anomaly.Components; // Sunrise-Edit
using Content.Shared.Fluids.Components; // Sunrise-Edit
using Content.Shared.FixedPoint; // Sunrise-Edit
using Content.Shared.Damage.Components; // Sunrise-Edit
using Content.Shared.Tag; // Sunrise-Edit
using Content.Shared.Xenoarchaeology.Artifact.Components; // Sunrise-Edit


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
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly EventManagerSystem _eventManager = default!;
    [Dependency] private readonly SharedCargoSystem _cargoSystem = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRoleSystem = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedBatterySystem _batterySystem = default!; // Sunrise-Edit

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
        // Sunrise-Edit: Scaled up initial BuildUp state transition time to 15-30 minutes
        component.StateTransitionTime = Timing.CurTime + TimeSpan.FromMinutes(_random.Next(15, 30));
        component.PacingState = StorytellerPacingState.BuildUp;
        component.ThreatBudget = 30f;

        // Sunrise-Edit
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

        // Sunrise-Edit: Load multipliers from StorytellerTypePrototype
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
        // Sunrise-Edit: Adjust duration multiplier based on StorytellerTypePrototype
        var durationMult = 1f;
        if (_protoManager.TryIndex<StorytellerTypePrototype>(comp.StorytellerType.ToString(), out var typeProto))
        {
            durationMult = typeProto.DurationMultiplier;
        }

        switch (comp.PacingState)
        {
            case StorytellerPacingState.Relaxation:
                comp.PacingState = StorytellerPacingState.BuildUp;
                // Sunrise-Edit: Scaled up standard Relaxation duration to 20-40 minutes
                comp.StateTransitionTime = Timing.CurTime + TimeSpan.FromMinutes(_random.Next(20, 40) * durationMult);
                break;
            case StorytellerPacingState.BuildUp:
                comp.PacingState = StorytellerPacingState.Peak;
                // Sunrise-Edit: Scaled up standard BuildUp duration to 15-30 minutes
                comp.StateTransitionTime = Timing.CurTime + TimeSpan.FromMinutes(_random.Next(15, 30) * durationMult);
                break;
            case StorytellerPacingState.Peak:
                comp.PacingState = StorytellerPacingState.Recovery;
                // Sunrise-Edit: Scaled up standard Peak duration to 12-24 minutes
                comp.StateTransitionTime = Timing.CurTime + TimeSpan.FromMinutes(_random.Next(12, 24) * durationMult);
                break;
            case StorytellerPacingState.Recovery:
                comp.PacingState = StorytellerPacingState.Relaxation;
                // Sunrise-Edit: Scaled up standard Recovery duration to 10-20 minutes
                comp.StateTransitionTime = Timing.CurTime + TimeSpan.FromMinutes(_random.Next(10, 20) * durationMult);
                break;
        }

        LogStorytellerState(comp, oldState);
    }

    private void EvaluateStoryteller(Entity<StorytellerRuleComponent> entity)
    {
        if (_roundEnd.IsRoundEndRequested())
            return;

        var metrics = CalculateStationMetrics();
        entity.Comp.CrewStress = CalculateCrewStress(metrics);

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
        var selected = PickEventFromEligible(eligibleEvents);
        if (selected == null)
            return;

        TriggerEvent(entity, selected.Value.Item1, selected.Value.Item2);
    }

    public StationMetrics CalculateStationMetrics()
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
        CalculateGridAtmosMetrics(out var breachRatio, out var hasDangerous, out var totalTiles);
        CalculatePowerGridMetrics(out var powerDeficit);
        var weaponsCount = CountCrewWeapons();
        CountAntagAndErt(out var antagCount, out var ertCount);
        GetSingularityTeslaStatus(out var singActive, out var singCont, out var tesActive, out var tesCont);
        var smIntegrity = GetSupermatterIntegrity();
        var researchTiers = GetUnlockedResearchCount();
        var distribution = GetCrewDistribution();
        GetPlayerFlowRates(out var joinRate, out var leaveRate);
        var ghostRolesCount = _ghostRoleSystem.GetGhostRoleCount();

        // Sunrise-Edit: Query new metrics (Anomalies, Artifacts, Puddles, Trash, Crew Damage)
        var anomaliesCount = 0;
        var anomalyQuery = EntityQueryEnumerator<AnomalyComponent>();
        while (anomalyQuery.MoveNext(out _, out _))
        {
            anomaliesCount++;
        }

        var activeArtifactsCount = 0;
        var artifactQuery = EntityQueryEnumerator<XenoArtifactComponent>();
        while (artifactQuery.MoveNext(out _, out _))
        {
            activeArtifactsCount++;
        }

        var puddlesCount = 0;
        var puddleQuery = EntityQueryEnumerator<PuddleComponent>();
        while (puddleQuery.MoveNext(out _, out _))
        {
            puddlesCount++;
        }

        var trashCount = 0;
        var tagQuery = EntityQueryEnumerator<TagComponent, TransformComponent>();
        while (tagQuery.MoveNext(out var uid, out var tag, out var xform))
        {
            if (tag.Tags.Contains("Trash") && xform.GridUid != null && xform.ParentUid == xform.GridUid)
            {
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
            HasDangerousGases = hasDangerous,
            PowerGridDeficitRatio = powerDeficit,
            CrewWeaponCount = weaponsCount,
            ActiveAntagonistCount = antagCount,
            ActiveErtCount = ertCount,
            SingularityActive = singActive,
            SingularityContained = singCont,
            TeslaActive = tesActive,
            TeslaContained = tesCont,
            SupermatterIntegrity = smIntegrity,
            UnlockedResearchTiers = researchTiers,
            CrewDistribution = distribution,
            PlayerJoinRate = joinRate,
            PlayerLeaveRate = leaveRate,
            AvailableGhostRoles = ghostRolesCount,

            // Sunrise-Edit: Populate new fields
            AnomaliesCount = anomaliesCount,
            ActiveArtifactsCount = activeArtifactsCount,
            PuddlesCount = puddlesCount,
            TrashCount = trashCount,
            AverageCrewDamage = averageCrewDamage,
            TotalStationTiles = totalTiles
        };
    }

    public float CalculateCrewStress(StationMetrics metrics)
    {
        if (metrics.TotalPlayers == 0)
            return 0f;

        // 1. Dead/Ghost ratio (up to 35 points of stress) - Sunrise-Edit: Balanced deadStress weight to 35
        float deadGhostRatio = (float)(metrics.DeadCount + metrics.GhostCount) / metrics.TotalPlayers;
        float deadStress = Math.Clamp(deadGhostRatio * 35f, 0f, 35f);

        // 2. Singularity/Tesla escape state (up to 30 points of stress) - Sunrise-Edit: Escape spikes stress immediately by 30!
        float containmentStress = 0f;
        if (metrics.SingularityActive)
        {
            containmentStress += metrics.SingularityContained ? 5f : 30f;
        }
        if (metrics.TeslaActive)
        {
            containmentStress += metrics.TeslaContained ? 5f : 30f;
        }
        containmentStress = Math.Clamp(containmentStress, 0f, 30f);

        // 3. Security force deficit (up to 10 points of stress) - Sunrise-Edit: Adjusted securityStress weight to 10
        float targetSecurity = metrics.TotalPlayers > 5 ? MathF.Max(1f, metrics.TotalPlayers * 0.1f) : 0f;
        float securityStress = 0f;
        if (targetSecurity > 0)
        {
            float deficit = MathF.Max(0f, targetSecurity - metrics.SecurityCount);
            securityStress = Math.Clamp((deficit / targetSecurity) * 10f, 0f, 10f);
        }

        // 4. Economy/RnD Distress (up to 10 points of stress) - Sunrise-Edit: Adjusted economyStress weight to 10
        float economyStress = 0f;
        if (metrics.CargoBalance < 1000) economyStress += 5f;
        if (metrics.SciencePoints < 2000) economyStress += 5f;

        // 5. Living Crew Damage (up to 5 points of stress) - Sunrise-Edit: Added average crew damage check
        float damageStress = Math.Clamp(metrics.AverageCrewDamage * 0.1f, 0f, 5f);

        // 6. Anomalies & Uncontained active artifacts (up to 6 points of stress) - Sunrise-Edit: Added anomalies and artifacts stress
        float anomalyStress = Math.Clamp((metrics.AnomaliesCount * 2f) + (metrics.ActiveArtifactsCount * 1.5f), 0f, 6f);

        // 7. Station Dirt & Mess (up to 4 points of stress) - Sunrise-Edit: Scaled density based on total station tiles
        float totalStationTiles = MathF.Max(100f, metrics.TotalStationTiles);
        float puddleDensity = metrics.PuddlesCount / totalStationTiles;
        float trashDensity = metrics.TrashCount / totalStationTiles;
        // Standard density threshold: 1 puddle per 330 tiles (~0.003), 1 trash per 160 tiles (~0.006)
        float messStress = Math.Clamp((puddleDensity / 0.003f) * 2f + (trashDensity / 0.006f) * 2f, 0f, 4f);

        float totalStress = deadStress + containmentStress + securityStress + economyStress + damageStress + anomalyStress + messStress;
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

            // Player count limits
            if (metrics.TotalPlayers < metadata.MinStress || comp.CrewStress < metadata.MinStress || comp.CrewStress > metadata.MaxStress)
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

    private (EntityPrototype, StorytellerMetadataPrototype)? PickEventFromEligible(Dictionary<EntityPrototype, StorytellerMetadataPrototype> available)
    {
        var totalWeight = 0f;
        foreach (var (proto, metadata) in available)
        {
            var baseWeight = 10f;
            if (proto.TryGetComponent<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
            {
                baseWeight = stationEvent.Weight;
            }
            totalWeight += baseWeight * metadata.WeightModifier;
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
            roll -= baseWeight * metadata.WeightModifier;
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

    private void CalculateGridAtmosMetrics(out float breachRatio, out bool hasDangerousGases, out int totalTiles)
    {
        breachRatio = 0f;
        hasDangerousGases = false;
        totalTiles = 0;

        var spacedTiles = 0;
        var dangerousTiles = 0;

        var query = EntityQueryEnumerator<GridAtmosphereComponent>();
        while (query.MoveNext(out _, out var atmos))
        {
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
                    // Sunrise-Edit: Expanded checks to include all dangerous/toxic/anesthetic/freezing gases
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
            // Sunrise-Edit: Raised threshold to 35 tiles to avoid false positives from atmospheric mixing rooms
            hasDangerousGases = dangerousTiles > 35;
        }
    }

    private void CalculatePowerGridMetrics(out float powerDeficitRatio)
    {
        var apcQuery = EntityQueryEnumerator<ApcComponent, BatteryComponent>();
        var totalApcs = 0;
        var deadApcs = 0;
        while (apcQuery.MoveNext(out var uid, out _, out var battery))
        {
            totalApcs++;
            // Sunrise-Edit
            if (_batterySystem.GetCharge((uid, battery)) <= battery.MaxCharge * 0.1f)
            {
                deadApcs++;
            }
        }
        powerDeficitRatio = totalApcs > 0 ? (float)deadApcs / totalApcs : 0f;
    }

    private int CountCrewWeapons()
    {
        var count = 0;
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
                    count++;
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
                    count++;
                }
            }
        }
        return count;
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

    private float GetSupermatterIntegrity()
    {
        var minIntegrity = 100f;
        var smQuery = EntityQueryEnumerator<SupermatterComponent>();
        var anySm = false;
        while (smQuery.MoveNext(out _, out var sm))
        {
            anySm = true;
            if (sm.Durability.Float() < minIntegrity)
            {
                minIntegrity = sm.Durability.Float();
            }
        }
        return anySm ? minIntegrity : 100f;
    }

    private int GetUnlockedResearchCount()
    {
        var count = 0;
        var techQuery = EntityQueryEnumerator<TechnologyDatabaseComponent>();
        while (techQuery.MoveNext(out _, out var techDb))
        {
            if (techDb.UnlockedTechnologies.Count > count)
            {
                count = techDb.UnlockedTechnologies.Count;
            }
        }
        return count;
    }

    private Dictionary<string, int> GetCrewDistribution()
    {
        var dist = new Dictionary<string, int>
        {
            { "Command", 0 },
            { "Security", 0 },
            { "Engineering", 0 },
            { "Medical", 0 },
            { "Science", 0 },
            { "Cargo", 0 },
            { "Civilian", 0 }
        };

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
    public bool HasDangerousGases;
    public float PowerGridDeficitRatio;
    public int CrewWeaponCount;
    public int ActiveAntagonistCount;
    public int ActiveErtCount;
    public bool SingularityActive;
    public bool SingularityContained;
    public bool TeslaActive;
    public bool TeslaContained;
    public float SupermatterIntegrity;
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
}
