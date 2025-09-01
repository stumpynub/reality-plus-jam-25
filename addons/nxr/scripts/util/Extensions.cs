using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Extensions
{
    public static T GetParentOrOwnerOfType<T>(this Node node)
    {
        //check parent first 
        if (node.GetParent() is T t)
            return t;
        if (node.Owner is T t2)
        {
            return t2;
        }

        return default;
    }

    public static bool DictionaryEquals<TKey, TValue>(
    this IDictionary<TKey, TValue> first,
    IDictionary<TKey, TValue> second)
    {
        if (first == null || second == null)
            return first == second;

        if (first.Count != second.Count)
            return false;

        var comparer = EqualityComparer<TValue>.Default;

        foreach (var kvp in first)
        {
            if (!second.TryGetValue(kvp.Key, out var secondValue))
                return false;

            if (!comparer.Equals(kvp.Value, secondValue))
                return false;
        }

        return true;
    }
}
