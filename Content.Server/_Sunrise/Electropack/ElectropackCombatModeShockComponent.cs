namespace Content.Server._Sunrise.Electropack;

/// <summary>
/// Маркер: бьёт носителя электрошоком при включении боевого режима,
/// используя существующий <see cref="Content.Shared.Trigger.Components.Effects.ShockOnTriggerComponent"/>.
/// Добавляется на рюкзак-электрошокер (<see cref="ClothingBackpackElectropack"/>).
/// </summary>
[RegisterComponent]
public sealed partial class ElectropackCombatModeShockComponent : Component
{
}
