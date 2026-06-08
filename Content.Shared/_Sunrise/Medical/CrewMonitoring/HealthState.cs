using Robust.Shared.Serialization;

namespace Content.Shared.Medical.CrewMonitoring;

/// <summary>
/// Crew monitor health category derived from suit sensor damage data.
/// </summary>
[Serializable, NetSerializable]
public enum CrewMonitoringHealthState
{
    Unknown,
    Healthy,
    Good,
    NotGreat,
    Bad,
    Terrible,
    Critical,
    Dead,
    Alive
}

public static class HealthStateHelper
{
    // The thresholds match the original crew monitor icon bands:
    // health0, health1, health2, health3, health4, then critical at 100% of the critical damage threshold.
    private const float GoodThreshold = 0.132f;
    private const float NotGreatThreshold = 0.36f;
    private const float BadThreshold = 0.6f;
    private const float TerribleThreshold = 0.83f;
    private const float CriticalThreshold = 1.0f;

    /// <summary>
    /// Converts suit sensor damage ratio into the matching crew monitor health category.
    /// </summary>
    /// <param name="damagePercentage">
    /// Damage divided by the critical damage threshold. 1.0 means 100% of the critical threshold.
    /// Null means the sensor mode does not provide health data.
    /// </param>
    /// <param name="isAlive">Whether the sensor owner is alive.</param>
    /// <returns>The health category used by crew monitor filtering and icons.</returns>
    public static CrewMonitoringHealthState GetHealthState(float? damagePercentage, bool isAlive)
    {
        if (!isAlive)
            return CrewMonitoringHealthState.Dead;

        if (damagePercentage == null)
            return CrewMonitoringHealthState.Alive;

        var damageRatio = damagePercentage.Value;

        if (damageRatio >= CriticalThreshold)
            return CrewMonitoringHealthState.Critical;
        if (damageRatio >= TerribleThreshold)
            return CrewMonitoringHealthState.Terrible;
        if (damageRatio >= BadThreshold)
            return CrewMonitoringHealthState.Bad;
        if (damageRatio >= NotGreatThreshold)
            return CrewMonitoringHealthState.NotGreat;
        if (damageRatio >= GoodThreshold)
            return CrewMonitoringHealthState.Good;
        return CrewMonitoringHealthState.Healthy;
    }
}
