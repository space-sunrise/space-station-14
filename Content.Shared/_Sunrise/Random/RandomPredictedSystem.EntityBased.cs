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
    public int NextForEntity(EntityUid ent, int minValue, int maxValue)
    {
        var random = GetOrCreateEntityRandom(ent);
        return random.Next(minValue, maxValue);
    }

    /// <summary>
    /// Возвращает случайное число с плавающей запятой для указанной сущности.
    /// </summary>
    public float NextFloatForEntity(EntityUid ent, float minValue = 0f, float maxValue = 1f)
    {
        var random = GetOrCreateEntityRandom(ent);
        return random.NextFloat(minValue, maxValue);
    }

    /// <summary>
    /// Возвращает случайное число двойной точности для указанной сущности.
    /// </summary>
    public double NextDoubleForEntity(EntityUid ent)
    {
        var random = GetOrCreateEntityRandom(ent);
        return random.NextDouble();
    }

    /// <summary>
    /// Возвращает true с заданной вероятностью для указанной сущности.
    /// </summary>
    public bool ProbForEntity(EntityUid ent, float chance)
    {
        var random = GetOrCreateEntityRandom(ent);
        return random.NextDouble() < chance;
    }

    /// <summary>
    /// Выбирает случайный элемент из списка для указанной сущности.
    /// </summary>
    public T PickForEntity<T>(EntityUid ent, IReadOnlyList<T> list)
    {
        var random = GetOrCreateEntityRandom(ent);
        var index = random.Next(list.Count);
        return list[index];
    }
}
