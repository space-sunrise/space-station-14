namespace Content.Server._Sunrise.Electropack;

/// <summary>
/// Маркер: бьёт носителя электрошоком при включении боевого режима,
/// используя существующий <see cref="Content.Shared.Trigger.Components.Effects.ShockOnTriggerComponent"/>.
/// Добавляется на рюкзак-электрошокер (<c>ClothingBackpackElectropack</c>).
/// </summary>
[RegisterComponent]
public sealed partial class TriggerOnCombatModeEnabledComponent : Component
{
}
