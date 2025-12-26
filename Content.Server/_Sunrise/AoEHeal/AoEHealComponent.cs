using Content.Shared.Damage;
using Content.Shared.Whitelist;

namespace Content.Server._Sunrise.AoEHeal;

/// <summary>
/// Этим компонентом помечать сущности, способные лечить по области
/// </summary>
[RegisterComponent]
public sealed partial class AoEHealComponent : Component
{
    /// <summary>
    /// Радиус в котором работает лечение
    /// </summary>
    [DataField]
    public float Range = 5f;

    /// <summary>
    /// Сколько урона лечится за 2 секунды
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage;

    /// <summary>
    /// На кого действует АоЕ
    /// </summary>
    [DataField("whitelist", required: true)]
    public EntityWhitelist EntityWhitelist;

    /// <summary>
    /// До какого уровня лечить сущность. null чтоб игнорировать проверку
    /// </summary>
    [DataField]
    public float? Threshold;

    /// <summary>
    /// Мы лечим только живое, или неживое тоже?
    /// </summary>
    [DataField]
    public bool AliveTargets = true;
}
