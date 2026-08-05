namespace Content.Shared._Sunrise.Flashbang;

/// <summary>
/// Маркер-компонент. Сущности с ним уязвимы к эффекту вспышки: их защита от экипировки
/// игнорируется, а <see cref="FlashbangRadiusOnTriggerComponent.IgnoreResistances"/>
/// принудительно применяется к ним независимо от настройки на источнике.
/// </summary>
[RegisterComponent]
public sealed partial class FlashbangVulnerableComponent : Component;
