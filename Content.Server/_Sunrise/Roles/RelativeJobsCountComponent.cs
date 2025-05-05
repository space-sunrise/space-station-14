using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.Roles;

/// <summary>
/// Компонент, вещающийся на станцию, чтобы увеличивать слоты на работы в зависимости от количества других работ
/// Увеличение происходит как только игрок заходит за роль
/// </summary>
[RegisterComponent]
public sealed partial class RelativeJobsCountComponent : Component
{
    /// <summary>
    /// Словарь работ, слоты которых будут зависеть от количества других работ.
    /// Пример реализации можно найти в прототипе id: PlanetPrison
    /// Ключ большого словаря - работа, которая получит слоты
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<JobPrototype>, Dictionary<ProtoId<JobPrototype>, int>> Jobs = new ();

    /// <summary>
    /// Пороговое значение для работ.
    /// TODO: Реализация получше, чтобы каждая работа могла получить свой макс каунт
    /// </summary>
    [DataField]
    public int? MaxCount;
}
