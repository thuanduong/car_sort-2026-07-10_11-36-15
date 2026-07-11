using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MonoBehaviorExtensions
{
    public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
    {
        if(!gameObject.TryGetComponent(typeof(T), out var t))
        {
            t = gameObject.AddComponent<T>();
        }
        return (T)t;
    }

    public static RectTransform GetRectTransform(this GameObject go)
    {
        return (RectTransform)go.transform;
    }

    public static RectTransform AsRectTransform(this Transform transform)
    {
        return (RectTransform)transform;
    }
}
