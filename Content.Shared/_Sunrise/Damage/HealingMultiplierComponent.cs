namespace Content.Shared._Sunrise.Damage;

/// <summary>
/// Домножает силу лечения (отрицательного урона), получаемого владельцем, независимо от источника лечения.
/// Значение меньше 1 ослабляет лечение, больше 1 усиливает.
/// </summary>
[RegisterComponent]
public sealed partial class HealingMultiplierComponent : Component
{
    /// <summary>
    /// Множитель силы лечения.
    /// </summary>
    [DataField]
    public float Multiplier = 1f;
}
