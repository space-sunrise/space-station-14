using Robust.Shared.Prototypes;
using Content.Server.Spawners.Components;

namespace Content.Server._Sunrise.Spawners.PreferredSpawn;

/// <summary>
/// Компонент, помечающий прототип точки спавна как имеющий предпочтительные типы спавна.
/// Определяет типы спавна, поддерживаемые этой конкретной точкой спавна (например, только для 'Job' или для 'LateJoin').
/// Это позволяет системе спавна более точно выбирать места для ролей.
/// </summary>
[RegisterComponent]
public sealed partial class PreferredSpawnComponent : Component
{
    /// <summary>
    /// Список типов спавна, которые эта точка спавна предпочитает.
    /// Например, точка спавна может быть предназначена только для первоначального появления (Job) или только для позднего присоединения (LateJoin).
    /// Это позволяет обеспечить гибкость в настройке спавнеров, когда конкретная точка может служить для нескольких целей,
    /// но с определенными приоритетами, которые обрабатывает система спавна.
    /// </summary>
    [DataField]
    public List<SpawnPointType> PreferredSpawnTypes { get; set; } = new() { SpawnPointType.Job };
}
