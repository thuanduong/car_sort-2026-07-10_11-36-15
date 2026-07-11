using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class EnumerableUtil
{
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
    {
        Random rnd = new Random();
        return list.OrderBy<T, int>((item) => rnd.Next());
    }

    public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
    {
        foreach (T item in enumeration)
        {
            action(item);
        }
    }
    
    public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T, int> action)
    {
        var enumerable = enumeration as T[] ?? enumeration.ToArray();
        var count = enumerable.Count();
        for (var i = 0; i < count; i++)
        {
            var index = i;
            var item = enumerable.ElementAt(index);
            action(item, index);
        }
    }

    public static T RandomElement<T>(this IEnumerable<T> enumeration)
    {
        return enumeration.OrderBy(x => UnityEngine.Random.value)
                          .FirstOrDefault();
    }
}
