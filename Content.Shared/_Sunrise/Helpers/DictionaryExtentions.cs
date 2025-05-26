namespace Content.Shared._Sunrise.Helpers;

public static class DictionaryExtensions
{
    public static void AddOrIncrement<TKey>(this Dictionary<TKey, int> dict, TKey key, int increment = 1)
        where TKey : notnull
    {
        if (dict.TryGetValue(key, out var currentValue))
        {
            dict[key] = currentValue + increment;
        }
        else
        {
            dict[key] = increment;
        }
    }

}
