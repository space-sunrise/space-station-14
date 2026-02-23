using System.Linq;
using Robust.Shared.Random;

namespace Content.Shared._Sunrise.Helpers;

public static class EnumerableExtensions
{
    /// <summary>
    /// Возвращает первые N элементов, где N = percentage от общего количества.
    /// </summary>
    /// <param name="source">Исходный список</param>
    /// <param name="percentage">Процент, который должен строго быть в [0, 1]</param>
    public static IEnumerable<T> TakePercentage<T>(
        this IList<T> source,
        float percentage)
    {
        if (source is null)
            throw new ArgumentException("Source list can not be null", nameof(source));

        if (percentage < 0f || percentage > 1f)
            throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage must be in range [0, 1].");

        var countToTake = (int)(source.Count * percentage);

        return source.Take(countToTake);
    }

    public static IList<T> ShuffleRobust<T>(this IList<T> source, IRobustRandom random)
    {
        random.Shuffle(source);
        return source;
    }
}
