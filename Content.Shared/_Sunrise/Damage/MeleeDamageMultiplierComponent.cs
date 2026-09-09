namespace Content.Shared._Sunrise.Damage;

/// <summary>
/// Домножает урон, наносимый владельцем в ближнем бою, независимо от используемого оружия (в т.ч. безоружные удары).
/// Значение меньше 1 ослабляет удар, больше 1 усиливает.
/// </summary>
[RegisterComponent]
public sealed partial class MeleeDamageMultiplierComponent : Component
{
    /// <summary>
    /// Множитель урона в ближнем бою.
    /// </summary>
    [DataField]
    public float Multiplier = 1f;
}
