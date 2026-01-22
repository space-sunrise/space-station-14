using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Random;

/// <summary>
/// Система для генерации предсказуемых случайных чисел.
/// Позволяет получать синхронизированные псевдорандомные числа между сервером и клиентом.
/// </summary>
/// <remarks>
/// Учтите, что результат БУДЕТ не похож на случайные числа из-за использования простой математической формулы для получения сида.
/// </remarks>
/// <seealso cref="SharedRandomExtensions"/>
public sealed partial class RandomPredictedSystem : EntitySystem
{
    /*
     * Основная часть системы.
     */

    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeTickBased();
    }

    /// <summary>
    /// Создает или получает экземпляр рандома для конкретной сущности.
    /// Сид зависит от текущего тика и ID сущности, что обеспечивает предсказуемость.
    /// </summary>
    /// <param name="uid"><see cref="EntityUid"/> на основе ID которого будет основываться сид для рандома.</param>
    private System.Random GetOrCreateEntityRandom(EntityUid uid)
    {
        var ent = GetNetEntity(uid);
        var seed = SharedRandomExtensions.HashCodeCombine(new List<int> { (int) _timing.CurTick.Value, ent.Id });
        var random = new System.Random(seed);

        return random;
    }
}

