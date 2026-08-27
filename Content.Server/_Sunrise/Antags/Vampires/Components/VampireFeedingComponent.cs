using Content.Shared.Damage;

namespace Content.Server._Sunrise.Antags.Vampires.Components;

/// <summary>
/// Состояние питания вампира.
/// </summary>
[RegisterComponent]
public sealed partial class VampireFeedingComponent : Component
{
    /// <summary>
    /// Интервал глотков.
    /// </summary>
    public TimeSpan SipInterval = TimeSpan.FromSeconds(1.25);

    /// <summary>
    /// Кровь за глоток.
    /// </summary>
    public float BloodGainPerSip = 10f;

    /// <summary>
    /// Потеря крови цели.
    /// </summary>
    public float TargetBloodDrainPerSip = 20f;

    /// <summary>
    /// Эффективность крови животных.
    /// </summary>
    public float AnimalEfficiency = 0.05f;

    /// <summary>
    /// Эффективность крови трупов.
    /// </summary>
    public float CorpseEfficiency = 0.1f;

    /// <summary>
    /// Урон укуса.
    /// </summary>
    public DamageSpecifier BiteDamage = new();

    /// <summary>
    /// Кровотечение от укуса.
    /// </summary>
    public float BiteBleedAmount = 1f;

    /// <summary>
    /// Эффективность по стадии гниения.
    /// </summary>
    public Dictionary<int, float> RotEfficiencyByStage = new()
    {
        [0] = 1f,
        [1] = 0.5f,
        [2] = 0.25f,
        [3] = 0.1f,
        [4] = 0f,
    };

    /// <summary>
    /// Дистанция кормления.
    /// </summary>
    public float BiteDistanceThreshold = 1.5f;

    /// <summary>
    /// Предел крови с цели.
    /// </summary>
    public float MaxBloodPerTarget = 200f;

    /// <summary>
    /// Лечение за глоток.
    /// </summary>
    public DamageSpecifier Healing = new();

    /// <summary>
    /// Активно ли кормление.
    /// </summary>
    public bool IsDrinking;

    /// <summary>
    /// Кровь с каждой цели.
    /// </summary>
    public Dictionary<EntityUid, float> BloodDrunkFromTargets = [];

    /// <summary>
    /// Укусы каждой цели.
    /// </summary>
    public Dictionary<EntityUid, int> BiteCountsByTarget = [];

    /// <summary>
    /// Остаток запаса крови.
    /// </summary>
    public float DrunkBloodRemainder;

    /// <summary>
    /// Остаток крови прогрессии.
    /// </summary>
    public float TotalBloodRemainder;

    /// <summary>
    /// Уникальные жертвы.
    /// </summary>
    public int UniqueHumanoidVictims;
}
