namespace Content.Shared.Medical.CrewMonitoring;

/// <summary>
/// Состояние здоровья существа.
/// </summary>
public enum HealthState
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
    public static HealthState GetHealthState(float? damagePercentage, bool isAlive)
    {
        if (!isAlive)
            return HealthState.Dead;

        if (damagePercentage == null)
            return HealthState.Unknown;

        float damage = damagePercentage.Value;

        if (damage >= 1.0f)
            return HealthState.Critical;
        if (damage >= 0.83f)
            return HealthState.Terrible;
        if (damage >= 0.6f)
            return HealthState.Bad;
        if (damage >= 0.36f)
            return HealthState.NotGreat;
        if (damage >= 0.132f)
            return HealthState.Good;
        return HealthState.Healthy;
    }
}
