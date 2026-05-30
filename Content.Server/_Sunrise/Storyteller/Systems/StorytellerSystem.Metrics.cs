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
    private static readonly Gauge CrewStressGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_crew_stress",
        "Current stress rating of the crew (0 to 100).");

    private static readonly Gauge ThreatBudgetGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_threat_budget",
        "Current threat budget available to spend on challenging events.");

    private static readonly Gauge PacingStateGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_pacing_state",
        "Current storyteller pacing state (0 = Relaxation, 1 = BuildUp, 2 = Peak, 3 = Recovery).");

    private static readonly Gauge AlivePlayersGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_alive_players",
        "Number of active alive players currently in-game.");

    private static readonly Gauge DeadPlayersGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_dead_players",
        "Number of dead players currently in-game.");

    private static readonly Gauge GhostPlayersGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_ghost_players",
        "Number of ghosts and observers currently in-game.");

    private static readonly Gauge SecurityCountGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_security_count",
        "Number of active alive security personnel.");

    private static readonly Gauge CargoBalanceGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_cargo_balance",
        "Sum of cargo accounts or current primary cargo balance.");

    private static readonly Gauge SciencePointsGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_science_points",
        "Sum of scientific research points currently available.");

    private static readonly Gauge AtmosBreachRatioGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_atmos_breach_ratio",
        "Ratio of spaced tiles to total grid tiles.");

    private static readonly Gauge DangerousGasesGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_dangerous_gases",
        "Whether dangerous levels of toxins or extreme temperature are active on the station (0 or 1).");

    private static readonly Gauge PowerDeficitRatioGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_power_deficit_ratio",
        "Ratio of drained/empty APC batteries to total APCs.");

    private static readonly Gauge CrewWeaponCountGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_crew_weapon_count",
        "Total number of active guns and high-damage melee weapons held by the crew.");

    private static readonly Gauge ActiveAntagonistCountGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_active_antagonist_count",
        "Number of active alive antagonists.");

    private static readonly Gauge ActiveErtCountGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_active_ert_count",
        "Number of active alive Emergency Response Team (ERT) and NT Command members.");

    private static readonly Gauge SingularityActiveGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_singularity_active",
        "Whether a singularity is currently active (0 or 1).");

    private static readonly Gauge TeslaActiveGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_tesla_active",
        "Whether a Tesla energy ball is currently active (0 or 1).");

    private static readonly Gauge SupermatterIntegrityGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_supermatter_integrity",
        "Minimum durability percentage of active supermatters (0 to 100).");

    private static readonly Gauge UnlockedResearchTiersGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_unlocked_research_tiers",
        "Total number of technologies researched by R&D.");

    private static readonly Gauge AvailableGhostRolesGauge = Prometheus.Metrics.CreateGauge(
        "ss14_storyteller_available_ghost_roles",
        "Total number of available ghost roles.");

    private static readonly Counter EventsTriggeredCounter = Prometheus.Metrics.CreateCounter(
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
        DangerousGasesGauge.Set(metrics.HasDangerousGases ? 1.0 : 0.0);
        PowerDeficitRatioGauge.Set(metrics.PowerGridDeficitRatio);
        CrewWeaponCountGauge.Set(metrics.CrewWeaponCount);
        ActiveAntagonistCountGauge.Set(metrics.ActiveAntagonistCount);
        ActiveErtCountGauge.Set(metrics.ActiveErtCount);
        SingularityActiveGauge.Set(metrics.SingularityActive ? 1.0 : 0.0);
        TeslaActiveGauge.Set(metrics.TeslaActive ? 1.0 : 0.0);
        SupermatterIntegrityGauge.Set(metrics.SupermatterIntegrity);
        UnlockedResearchTiersGauge.Set(metrics.UnlockedResearchTiers);
        AvailableGhostRolesGauge.Set(metrics.AvailableGhostRoles);
    }

    private void LogStorytellerState(StorytellerRuleComponent comp, StorytellerPacingState? oldState)
    {
        if (!_cfg.GetCVar(SunriseCCVars.StorytellerTelemetryEnabled))
            return;

        var json = $"{{\"event\":\"storyteller_state_changed\",\"old_state\":\"{oldState}\",\"new_state\":\"{comp.PacingState}\",\"crew_stress\":{comp.CrewStress:F1},\"threat_budget\":{comp.ThreatBudget:F1}}}";
        _sawmill.Info(json);
    }

    private void LogTelemetryTick(StorytellerRuleComponent comp, StationMetrics metrics)
    {
        if (!_cfg.GetCVar(SunriseCCVars.StorytellerTelemetryEnabled))
            return;

        var json = $"{{" +
                   $"\"event\":\"storyteller_tick\"," +
                   $"\"state\":\"{comp.PacingState}\"," +
                   $"\"crew_stress\":{comp.CrewStress:F2}," +
                   $"\"threat_budget\":{comp.ThreatBudget:F2}," +
                   $"\"metrics\":{{" +
                   $"\"total_players\":{metrics.TotalPlayers}," +
                   $"\"alive\":{metrics.AliveCount}," +
                   $"\"dead\":{metrics.DeadCount}," +
                   $"\"ghosts\":{metrics.GhostCount}," +
                   $"\"security\":{metrics.SecurityCount}," +
                   $"\"cargo_balance\":{metrics.CargoBalance}," +
                   $"\"science_points\":{metrics.SciencePoints}," +
                   $"\"atmos_breach_ratio\":{metrics.AtmosphereBreachRatio:F4}," +
                   $"\"dangerous_gases\":{(metrics.HasDangerousGases ? "true" : "false")}," +
                   $"\"power_deficit_ratio\":{metrics.PowerGridDeficitRatio:F4}," +
                   $"\"crew_weapon_count\":{metrics.CrewWeaponCount}," +
                   $"\"active_antagonist_count\":{metrics.ActiveAntagonistCount}," +
                   $"\"active_ert_count\":{metrics.ActiveErtCount}," +
                   $"\"singularity_active\":{(metrics.SingularityActive ? "true" : "false")}," +
                   $"\"singularity_contained\":{(metrics.SingularityContained ? "true" : "false")}," +
                   $"\"tesla_active\":{(metrics.TeslaActive ? "true" : "false")}," +
                   $"\"tesla_contained\":{(metrics.TeslaContained ? "true" : "false")}," +
                   $"\"supermatter_integrity\":{metrics.SupermatterIntegrity:F2}," +
                   $"\"unlocked_research_tiers\":{metrics.UnlockedResearchTiers}," +
                   $"\"player_join_rate\":{metrics.PlayerJoinRate:F2}," +
                   $"\"player_leave_rate\":{metrics.PlayerLeaveRate:F2}," +
                   $"\"available_ghost_roles\":{metrics.AvailableGhostRoles}," +
                   $"\"anomalies\":{metrics.AnomaliesCount}," +
                   $"\"active_artifacts\":{metrics.ActiveArtifactsCount}," +
                   $"\"puddles\":{metrics.PuddlesCount}," +
                   $"\"trash\":{metrics.TrashCount}," +
                   $"\"average_crew_damage\":{metrics.AverageCrewDamage:F2}" +
                   $"}}" +
                   $"}}";
        _sawmill.Info(json);
    }

    private void RecordEventTriggered(string eventId, StorytellerMetadataPrototype metadata)
    {
        EventsTriggeredCounter.WithLabels(eventId, metadata.ThreatType.ToString()).Inc();

        if (!_cfg.GetCVar(SunriseCCVars.StorytellerTelemetryEnabled))
            return;

        var json = $"{{\"event\":\"storyteller_event_triggered\",\"rule_id\":\"{eventId}\",\"threat_type\":\"{metadata.ThreatType}\",\"cost\":{metadata.ThreatCost:F1},\"stress_reduction\":{metadata.StressReduction:F1}}}";
        _sawmill.Info(json);
    }
}
