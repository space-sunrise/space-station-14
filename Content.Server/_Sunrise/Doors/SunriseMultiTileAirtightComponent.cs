namespace Content.Server._Sunrise.Doors.Components;

/// <summary>
///     Сейчас используется только под двойные, тройные шлюзы
///     Спавнит блокеры по боковым частям относительно главного тайла
/// </summary>
[RegisterComponent]
public sealed partial class SunriseMultiTileAirtightComponent : Component
{
    /// <summary>
    ///     Список оффсетов в локальных координатах двери, на каких тайлах должны появиться блокеры
    ///     Оффсет задает относительно главной точки двери
    /// </summary>
    [DataField(required: true)]
    public List<Vector2i> ExtraTiles = new();

    /// <summary>
    ///    Cписок заспавненных блокеров, используется системой для обновления airtight
    /// </summary>
    public List<EntityUid> Blockers = new();
}

