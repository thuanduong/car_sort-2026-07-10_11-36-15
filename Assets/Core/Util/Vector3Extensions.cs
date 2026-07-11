using System;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extensions
{
    public static Vector2 XZ(this Vector3 v)
    {
        return new Vector2(v.x, v.z);
    }

    public static Vector3 X0Z(this Vector3 v)
    {
        return new Vector3(v.x,0, v.z);
    }
    
    public static Vector3 SetX(this Vector3 v, float val)
    {
        return new Vector3(val,v.y, v.z);
    }
    
    public static Vector3 SetY(this Vector3 v, float val)
    {
        return new Vector3(v.x,val, v.z);
    }
    
    public static Vector3 SetZ(this Vector3 v, float val)
    {
        return new Vector3(v.x,v.y, val);
    }

    public static void SetX(this Transform transform,
                            float val)
    {
        transform.position = transform.position.SetX(val);
    }
    
    public static void SetY(this Transform transform,
                            float val)
    {
        transform.position = transform.position.SetY(val);
    }
    
    public static void SetZ(this Transform transform,
                            float val)
    {
        transform.position = transform.position.SetZ(val);
    }
    
    public static Vector3 RandomPointInBounds(this Bounds bounds) {
        return new Vector3(
            UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
            UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
            UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
        );
    }
    
    public static Vector3 RandomPointInBounds(this BoxCollider boxCollider)
    {
        var size = Vector3.Scale(boxCollider.size , boxCollider.transform.lossyScale);
        var boxColliderCenter = boxCollider.transform.position + boxCollider.center;
        var min = boxColliderCenter - size / 2;
        var max = boxColliderCenter + size / 2;
        return new Vector3(
            UnityEngine.Random.Range(min.x, max.x),
            UnityEngine.Random.Range(min.y, max.y),
            UnityEngine.Random.Range(min.z, max.z));
    }

    public static float Random(this Vector2 range)
    {
        return UnityEngine.Random.Range(range.x, range.y);
    }
    
    public static float Map(float s, float a1, float a2, float b1, float b2)
    {
        if (Math.Abs(a2 - a1) < 0.0001f)
        {
            return (b1 + b2) * 0.5f;
        }
        else
        {
            return b1 + (s-a1)*(b2-b1)/(a2-a1);    
        }
    }
    
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
}
