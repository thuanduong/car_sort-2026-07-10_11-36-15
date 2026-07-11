using System;
using System.Collections.Generic;
public static class RandomUtil
{
    public static T GetRandom<T>(this ICollection<T> collection) 
    {
        if (collection == null)
            return default(T);
        int t = UnityEngine.Random.Range(0, collection.Count);
        foreach (T element in collection) {
            if (t == 0)
                return element;
            t--;
        }
        return default(T);
    }

    public static int BoolToBinary(bool b) => b ? 1 : 0;

    public static T Next<T>(this T src) where T : struct
    {
        if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));

        T[] Arr = (T[])Enum.GetValues(src.GetType());
        int j = Array.IndexOf<T>(Arr, src) + 1;
        return (Arr.Length == j) ? Arr[0] : Arr[j];
    }
}