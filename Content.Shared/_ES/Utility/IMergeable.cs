namespace Content.Shared._ES.Utility;

/// <summary>
///     An interface for data that can be merged together, and optionally cancel each-other out.
///     The simplest example of this would be an integer wrapper type, where you want them to merge together
///     if they share a key in a dictionary, and be removed from the dictionary if they're equal to 0.
/// </summary>
/// <typeparam name="TSelf">The type this interface is implemented on.</typeparam>
public interface IMergeable<in TSelf>
{
    /// <summary>
    ///     Merges this object with the other given object. Returns false if the two cancelled out and should
    ///     both be discarded.
    /// </summary>
    public bool Merge(TSelf other);
}

public static class MergeExtensions
{
    /// <summary>
    ///     Add the given value into the dictionary with the given key, calling <see cref="IMergeable{TSelf}.Merge"/>
    ///     to merge the values together if something is already at that position.
    /// </summary>
    public static void MergeValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        where TValue: IMergeable<TValue>
    {
        if (dict.TryGetValue(key, out var other))
        {
            var keep = other.Merge(value);
            if (!keep)
                dict.Remove(key);
        }
        else
        {
            dict.Add(key, value);
        }
    }
}
