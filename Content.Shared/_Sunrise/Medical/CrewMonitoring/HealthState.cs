namespace Content.Shared.Medical.CrewMonitoring;

/// <summary>
/// Состояние здоровья существа.
/// </summary>
public enum CrewMonitoringHealthState
{
    Unknown,    // нет данных о здоровье (датчики не в health-режиме)
    Healthy,    // 0% – 13.2%
    Good,       // 13.2% – 36%
    NotGreat,   // 36% – 60%
    Bad,        // 60% – 83%
    Terrible,   // 83% – 100%
    Critical,   // >= 100%
    Dead        // мёртв
}

public static class HealthStateHelper
{
    /// <summary>
    /// Определяет состояние здоровья по проценту урона и признаку жизни.
    /// </summary>
    /// <param name="damagePercentage">Процент урона (0..∞). null = нет данных о здоровье.</param>
    /// <param name="isAlive">Жив ли существо.</param>
    /// <returns>Состояние здоровья.</returns>
    public static CrewMonitoringHealthState GetHealthState(float? damagePercentage, bool isAlive)
    {
        if (!isAlive)
            return CrewMonitoringHealthState.Dead;

        if (damagePercentage == null)
            return CrewMonitoringHealthState.Unknown;

        float damage = damagePercentage.Value;

        if (damage >= 1.0f)
            return CrewMonitoringHealthState.Critical;
        if (damage >= 0.83f)
            return CrewMonitoringHealthState.Terrible;
        if (damage >= 0.6f)
            return CrewMonitoringHealthState.Bad;
        if (damage >= 0.36f)
            return CrewMonitoringHealthState.NotGreat;
        if (damage >= 0.132f)
            return CrewMonitoringHealthState.Good;
        return CrewMonitoringHealthState.Healthy;
    }
}
