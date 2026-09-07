namespace Content.Shared._Sunrise.Lathe;

/// <summary>
/// Рассылается после того, как лейт физически заспавнил и попытался слить в соседние стаки готовую продукцию.
/// Новый, чисто аддитивный хук в ванильном <c>LatheSystem.FinishProducing</c> — сама логика на него не завязана,
/// подписчики (например, авто-скидывание в сило) реализуются отдельно.
/// </summary>
[ByRefEvent]
public readonly record struct SunriseLatheProductPrintedEvent(EntityUid Result);
