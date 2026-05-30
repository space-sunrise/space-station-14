using System.Globalization;
using Prometheus;
using Content.Server._Sunrise.Storyteller.Components;
using Content.Shared._Sunrise.Storyteller.Prototypes;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Log;

namespace Content.Server._Sunrise.Storyteller.Systems;

public sealed partial class StorytellerSystem
{
    private ISawmill _sawmill = default!;

    // Prometheus Metrics
    private static readonly Gauge CrewStressGauge = Metrics.CreateGauge(
        "ss14_storyteller_crew_stress",
        "Current stress rating of the crew (0 to 100).");

    private static readonly Gauge ThreatBudgetGauge = Metrics.CreateGauge(
        "ss14_storyteller_threat_budget",
        "Current threat budget available to spend on challenging events.");

    private static readonly Gauge PacingStateGauge = Metrics.CreateGauge(
        "ss14_storyteller_pacing_state",
        "Current storyteller pacing state (0 = Relaxation, 1 = BuildUp, 2 = Peak, 3 = Recovery).");

    private static readonly Gauge AlivePlayersGauge = Metrics.CreateGauge(
        "ss14_storyteller_alive_players",
        "Number of active alive players currently in-game.");

    private static readonly Gauge DeadPlayersGauge = Metrics.CreateGauge(
        "ss14_storyteller_dead_players",
        "Number of dead players currently in-game.");

    private static readonly Gauge GhostPlayersGauge = Metrics.CreateGauge(
        "ss14_storyteller_ghost_players",
        "Number of ghosts and observers currently in-game.");

    private static readonly Gauge SecurityCountGauge = Metrics.CreateGauge(
        "ss14_storyteller_security_count",
        "Number of active alive security personnel.");

    private static readonly Gauge CargoBalanceGauge = Metrics.CreateGauge(
        "ss14_storyteller_cargo_balance",
        "Sum of cargo accounts or current primary cargo balance.");

    private static readonly Gauge SciencePointsGauge = Metrics.CreateGauge(
        "ss14_storyteller_science_points",
        "Sum of scientific research points currently available.");

    private static readonly Gauge AtmosBreachRatioGauge = Metrics.CreateGauge(
        "ss14_storyteller_atmos_breach_ratio",
        "Ratio of spaced tiles to total grid tiles.");

    private static readonly Gauge DangerousGasesGauge = Metrics.CreateGauge(
        "ss14_storyteller_dangerous_gases",
        "Whether dangerous levels of toxins or extreme temperature are active on the station (0 or 1).");

    private static readonly Gauge PowerDeficitRatioGauge = Metrics.CreateGauge(
        "ss14_storyteller_power_deficit_ratio",
        "Ratio of drained/empty APC batteries to total APCs.");

    private static readonly Gauge CrewWeaponCountGauge = Metrics.CreateGauge(
        "ss14_storyteller_crew_weapon_count",
        "Total number of unique living crew members carrying at least one active weapon.");

    private static readonly Gauge ActiveAntagonistCountGauge = Metrics.CreateGauge(
        "ss14_storyteller_active_antagonist_count",
        "Number of active alive antagonists.");

    private static readonly Gauge ActiveErtCountGauge = Metrics.CreateGauge(
        "ss14_storyteller_active_ert_count",
        "Number of active alive Emergency Response Team (ERT) and NT Command members.");

    private static readonly Gauge SingularityActiveGauge = Metrics.CreateGauge(
        "ss14_storyteller_singularity_active",
        "Whether a singularity is currently active (0 or 1).");

    private static readonly Gauge TeslaActiveGauge = Metrics.CreateGauge(
        "ss14_storyteller_tesla_active",
        "Whether a Tesla energy ball is currently active (0 or 1).");

    private static readonly Gauge SupermatterIntegrityGauge = Metrics.CreateGauge(
        "ss14_storyteller_supermatter_integrity",
        "Minimum durability percentage of active supermatters (0 to 100).");

    private static readonly Gauge UnlockedResearchTiersGauge = Metrics.CreateGauge(
        "ss14_storyteller_unlocked_research_tiers",
        "Total number of technologies researched by R&D.");

    private static readonly Gauge AvailableGhostRolesGauge = Metrics.CreateGauge(
        "ss14_storyteller_available_ghost_roles",
        "Total number of available ghost roles.");


    private static readonly Gauge AnomaliesCountGauge = Metrics.CreateGauge(
        "ss14_storyteller_anomalies_count",
        "Total number of active anomalies.");

    private static readonly Gauge ActiveArtifactsCountGauge = Metrics.CreateGauge(
        "ss14_storyteller_active_artifacts_count",
        "Total number of active uncontained artifacts.");

    private static readonly Gauge PuddlesCountGauge = Metrics.CreateGauge(
        "ss14_storyteller_puddles_count",
        "Total number of puddles on the station.");

    private static readonly Gauge TrashCountGauge = Metrics.CreateGauge(
        "ss14_storyteller_trash_count",
        "Total number of trash items on the station grid.");

    private static readonly Gauge AverageCrewDamageGauge = Metrics.CreateGauge(
        "ss14_storyteller_average_crew_damage",
        "Average damage across the living crew members.");

    private static readonly Gauge StationStrengthGauge = Metrics.CreateGauge(
        "ss14_storyteller_station_strength",
        "Calculated defensive and technological strength score of the station.");


    private static readonly Gauge StressDeadGauge = Metrics.CreateGauge("ss14_storyteller_stress_dead", "Stress from dead and ghost players.");
    private static readonly Gauge StressContainmentGauge = Metrics.CreateGauge("ss14_storyteller_stress_containment", "Stress from singularity or tesla containment breach.");
    private static readonly Gauge StressSecurityGauge = Metrics.CreateGauge("ss14_storyteller_stress_security", "Stress from security force deficit.");
    private static readonly Gauge StressEconomyGauge = Metrics.CreateGauge("ss14_storyteller_stress_economy", "Stress from cargo budget deficit.");
    private static readonly Gauge StressDamageGauge = Metrics.CreateGauge("ss14_storyteller_stress_damage", "Stress from average crew damage.");
    private static readonly Gauge StressAnomalyGauge = Metrics.CreateGauge("ss14_storyteller_stress_anomaly", "Stress from active anomalies and uncontained artifacts.");
    private static readonly Gauge StressMessGauge = Metrics.CreateGauge("ss14_storyteller_stress_mess", "Stress from puddles and trash on the station.");

    private static readonly Gauge StrengthArmedCrewGauge = Metrics.CreateGauge("ss14_storyteller_strength_armed_crew", "Station strength from peaceful armed crew members.");
    private static readonly Gauge StrengthSecurityGauge = Metrics.CreateGauge("ss14_storyteller_strength_security", "Station strength from security personnel.");
    private static readonly Gauge StrengthCargoGauge = Metrics.CreateGauge("ss14_storyteller_strength_cargo", "Station strength from cargo budget balance.");
    private static readonly Gauge StrengthTechnologyGauge = Metrics.CreateGauge("ss14_storyteller_strength_technology", "Station strength from researched technologies.");
    private static readonly Gauge StressPowerGauge = Metrics.CreateGauge("ss14_storyteller_stress_power", "Stress from power grid deficit.");
    private static readonly Gauge StressBreachGauge = Metrics.CreateGauge("ss14_storyteller_stress_breach", "Stress from atmosphere breach (spaced tiles).");
    private static readonly Gauge StressGasGauge = Metrics.CreateGauge("ss14_storyteller_stress_gas", "Stress from active toxic/dangerous gases.");

    private static readonly Counter EventsTriggeredCounter = Metrics.CreateCounter(
        "ss14_storyteller_events_triggered_total",
        "Total number of events triggered by the storyteller.",
        new CounterConfiguration
        {
            LabelNames = new[] { "event_id", "threat_type" }
        });

    private void InitializeMetrics()
    {
        _sawmill = Logger.GetSawmill("storyteller");
    }

    private void UpdatePrometheusGauges(StorytellerRuleComponent comp, StationMetrics metrics)
    {
        CrewStressGauge.Set(comp.CrewStress);
        ThreatBudgetGauge.Set(comp.ThreatBudget);
        PacingStateGauge.Set((double)comp.PacingState);
        AlivePlayersGauge.Set(metrics.AliveCount);
        DeadPlayersGauge.Set(metrics.DeadCount);
        GhostPlayersGauge.Set(metrics.GhostCount);
        SecurityCountGauge.Set(metrics.SecurityCount);
        CargoBalanceGauge.Set(metrics.CargoBalance);
        SciencePointsGauge.Set(metrics.SciencePoints);

        AtmosBreachRatioGauge.Set(metrics.AtmosphereBreachRatio);
        DangerousGasesGauge.Set(metrics.DangerousGasesRatio);
        PowerDeficitRatioGauge.Set(metrics.PowerGridDeficitRatio);
        CrewWeaponCountGauge.Set(metrics.CrewWeaponCount);
        ActiveAntagonistCountGauge.Set(metrics.ActiveAntagonistCount);
        ActiveErtCountGauge.Set(metrics.ActiveErtCount);
        SingularityActiveGauge.Set(metrics.SingularityActive ? 1.0 : 0.0);
        TeslaActiveGauge.Set(metrics.TeslaActive ? 1.0 : 0.0);
        SupermatterIntegrityGauge.Set(metrics.SupermatterIntegrity);
        UnlockedResearchTiersGauge.Set(metrics.UnlockedResearchTiers);
        AvailableGhostRolesGauge.Set(metrics.AvailableGhostRoles);


        AnomaliesCountGauge.Set(metrics.AnomaliesCount);
        ActiveArtifactsCountGauge.Set(metrics.ActiveArtifactsCount);
        PuddlesCountGauge.Set(metrics.PuddlesCount);
        TrashCountGauge.Set(metrics.TrashCount);
        AverageCrewDamageGauge.Set(metrics.AverageCrewDamage);
        StationStrengthGauge.Set(metrics.StationStrength);

        // Update individual components
        StressDeadGauge.Set(metrics.StressDead);
        StressContainmentGauge.Set(metrics.StressContainment);
        StressSecurityGauge.Set(metrics.StressSecurity);
        StressEconomyGauge.Set(metrics.StressEconomy);
        StressDamageGauge.Set(metrics.StressDamage);
        StressAnomalyGauge.Set(metrics.StressAnomaly);
        StressMessGauge.Set(metrics.StressMess);

        StrengthArmedCrewGauge.Set(metrics.StrengthArmedCrew);
        StrengthSecurityGauge.Set(metrics.StrengthSecurity);
        StrengthCargoGauge.Set(metrics.StrengthCargo);
        StrengthTechnologyGauge.Set(metrics.StrengthTechnology);

        // Update environmental stress components
        StressPowerGauge.Set(metrics.StressPower);
        StressBreachGauge.Set(metrics.StressBreach);
        StressGasGauge.Set(metrics.StressGas);

    }

    private void LogStorytellerState(StorytellerRuleComponent comp, StorytellerPacingState? oldState)
    {
        if (!_cfg.GetCVar(SunriseCCVars.StorytellerTelemetryEnabled))
            return;

        _sawmill.Info($"State changed: {oldState} -> {comp.PacingState}. Crew stress: {comp.CrewStress.ToString("F1", CultureInfo.InvariantCulture)}, Threat budget: {comp.ThreatBudget.ToString("F1", CultureInfo.InvariantCulture)}");
    }

    private void LogTelemetryTick(StorytellerRuleComponent comp, StationMetrics metrics)
    {
        if (!_cfg.GetCVar(SunriseCCVars.StorytellerTelemetryEnabled))
            return;

        _sawmill.Info($"Tick - State: {comp.PacingState}, Crew Stress: {comp.CrewStress.ToString("F2", CultureInfo.InvariantCulture)}, Threat Budget: {comp.ThreatBudget.ToString("F2", CultureInfo.InvariantCulture)}, " +
                      $"Players: {metrics.AliveCount}/{metrics.TotalPlayers} (Dead: {metrics.DeadCount}, Ghosts: {metrics.GhostCount}, Sec: {metrics.SecurityCount}), " +
                      $"Economy: Cargo Balance {metrics.CargoBalance}, Science Points {metrics.SciencePoints}, " +
                      $"Atmos: Breach Ratio {metrics.AtmosphereBreachRatio.ToString("F4", CultureInfo.InvariantCulture)}, Dangerous Gases Ratio: {metrics.DangerousGasesRatio.ToString("F4", CultureInfo.InvariantCulture)}, " +
                      $"Power Deficit Ratio: {metrics.PowerGridDeficitRatio.ToString("F4", CultureInfo.InvariantCulture)}, " +
                      $"Weapons: {metrics.CrewWeaponCount}, Antags: {metrics.ActiveAntagonistCount}, ERT: {metrics.ActiveErtCount}, Station Strength: {metrics.StationStrength}, " +
                      $"Singularity: Active {metrics.SingularityActive}/Contained {metrics.SingularityContained}, " +
                      $"Tesla: Active {metrics.TeslaActive}/Contained {metrics.TeslaContained}, " +
                      $"SM Integrity: {metrics.SupermatterIntegrity.ToString("F2", CultureInfo.InvariantCulture)}, " +
                      $"Research Tiers: {metrics.UnlockedResearchTiers}, Ghost Roles: {metrics.AvailableGhostRoles}, " +
                      $"Anomalies: {metrics.AnomaliesCount}, Artifacts: {metrics.ActiveArtifactsCount}, " +
                      $"Puddles: {metrics.PuddlesCount}, Trash: {metrics.TrashCount}, Avg Damage: {metrics.AverageCrewDamage.ToString("F2", CultureInfo.InvariantCulture)}");
    }

    private void RecordEventTriggered(string eventId, StorytellerMetadataPrototype metadata)
    {
        EventsTriggeredCounter.WithLabels(eventId, metadata.ThreatType.ToString()).Inc();

        if (!_cfg.GetCVar(SunriseCCVars.StorytellerTelemetryEnabled))
            return;

        Log.Info($"[Storyteller] Triggered event: {eventId} (Threat Type: {metadata.ThreatType}, Cost: {metadata.ThreatCost.ToString("F1", CultureInfo.InvariantCulture)}, Stress Reduction: {metadata.StressReduction.ToString("F1", CultureInfo.InvariantCulture)})");
    }
}
