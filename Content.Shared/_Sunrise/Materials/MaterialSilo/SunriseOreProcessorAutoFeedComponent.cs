namespace Content.Shared._Sunrise.Materials.MaterialSilo;

/// <summary>
/// Маркер: сущность при подключении к <see cref="SunriseMaterialSiloComponent"/> через
/// <see cref="SunriseMaterialSiloClientComponent"/> сразу отправляет свежую продукцию лейта в силос,
/// а не разбрасывает её физически рядом с собой.
/// </summary>
[RegisterComponent]
public sealed partial class SunriseOreProcessorAutoFeedComponent : Component;
