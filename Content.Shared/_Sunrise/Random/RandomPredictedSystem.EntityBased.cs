using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared._Sunrise.Random;

public sealed partial class RandomPredictedSystem
{
    /*
     * Часть системы, отвечающая за рандом на основе EntityUid сущности и текущего тика.
     * Более надежда в использовании.
     */

    /// <summary>
    /// Возвращает случайное целое число для указанной сущности.
    /// </summary>
    /// <returns>Случайно число типа <see cref="int"/> в заданном диапазоне</returns>
    [PublicAPI]
    public int NextForEntity(EntityUid ent, int minValue = 0, int maxValue = int.MaxValue)
    {
        var random = GetOrCreateEntityRandom(ent);
        return random.Next(minValue, maxValue);
    }

    /// <summary>
    /// <inheritdoc cref="NextForEntity(EntityUid, int, int)"/>
    /// </summary>
    [PublicAPI]
    public int NextForEntity(List<EntityUid> entities, int minValue = 0, int maxValue = int.MaxValue)
    {
        var random = GetOrCreateEntityRandom(entities);
        return random.Next(minValue, maxValue);
    }

    /// <summary>
    /// Возвращает случайное число с плавающей запятой для указанной сущности.
    /// </summary>
    /// <returns>Случайно число типа <see cref="float"/> в заданном диапазоне</returns>
    [PublicAPI]
    public float NextFloatForEntity(EntityUid ent, float minValue = 0f, float maxValue = 1f)
    {
        var random = GetOrCreateEntityRandom(ent);
        return random.NextFloat(minValue, maxValue);
    }

    /// <summary>
    /// <inheritdoc cref="NextFloatForEntity(EntityUid, float, float)"/>
    /// </summary>
    [PublicAPI]
    public float NextFloatForEntity(List<EntityUid> entities, float minValue = 0f, float maxValue = 1f)
    {
        var random = GetOrCreateEntityRandom(entities);
        return random.NextFloat(minValue, maxValue);
    }

    /// <summary>
    /// Возвращает случайное число двойной точности для указанной сущности.
    /// </summary>
    /// <returns>Случайно число типа <see cref="double"/> в диапазоне [0, 1)</returns>
    [PublicAPI]
    public double NextDoubleForEntity(EntityUid ent)
    {
        var random = GetOrCreateEntityRandom(ent);
        return random.NextDouble();
    }

    /// <summary>
    /// <inheritdoc cref="NextDoubleForEntity(EntityUid)"/>
    /// </summary>
    [PublicAPI]
    public double NextDoubleForEntity(List<EntityUid> entities)
    {
        var random = GetOrCreateEntityRandom(entities);
        return random.NextDouble();
    }

    /// <summary>
    /// Возвращает true с заданной вероятностью для указанной сущности.
    /// </summary>
    /// <remarks>
    /// Шанс обязан быть в диапазоне [0, 1]!
    /// </remarks>
    /// <returns>Прокнули ли переданный шанс</returns>
    [PublicAPI]
    public bool ProbForEntity(EntityUid ent, float chance = 0.5f)
    {
        var random = GetOrCreateEntityRandom(ent);
        return random.NextDouble() < chance;
    }

    /// <summary>
    /// <inheritdoc cref="ProbForEntity(EntityUid, float)"/>
    /// </summary>
    [PublicAPI]
    public bool ProbForEntity(List<EntityUid> entities, float chance = 0.5f)
    {
        var random = GetOrCreateEntityRandom(entities);
        return random.NextDouble() < chance;
    }

    /// <summary>
    /// Выбирает случайный элемент из списка для указанной сущности.
    /// </summary>
    /// <returns>Случайный элемент из списка <see cref="list"/></returns>
    /// TODO: Здесь кажется скрывается миспредикт, но я его не вижу. Почините, если это так или уберите эту плашку.
    [PublicAPI]
    public T PickForEntity<T>(EntityUid ent, IReadOnlyList<T> list)
    {
        var random = GetOrCreateEntityRandom(ent);
        var index = random.Next(list.Count);
        return list[index];
    }

    /// <summary>
    /// <inheritdoc cref="PickForEntity{T}(EntityUid, IReadOnlyList{T})"/>
    /// </summary>
    /// TODO: Здесь кажется скрывается миспредикт, но я его не вижу. Почините, если это так или уберите эту плашку.
    [PublicAPI]
    public T PickForEntity<T>(List<EntityUid> entities, IReadOnlyList<T> list)
    {
        var random = GetOrCreateEntityRandom(entities);
        var index = random.Next(list.Count);
        return list[index];
    }
}
