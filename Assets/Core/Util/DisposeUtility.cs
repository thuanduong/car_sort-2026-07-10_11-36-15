using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

public static class DisposeUtility
{
    public static void SafeDispose<T>(ref T disposable) where T : IDisposable
    {
        if(disposable is Component component)
        {
            Object.Destroy(component?.gameObject);
        }
        disposable?.Dispose();
        disposable = default;
    }
    
    public static void SafeDisposeComponent<T>(ref T monoBehaviour) where T : Component
    {
        if (monoBehaviour == default) return;
        Object.Destroy(monoBehaviour.gameObject);
        monoBehaviour = default;
    }

    public static void SafeDispose(ref CancellationTokenSource cts)
    {
        cts.SafeCancelAndDispose();
        cts = default;
    }
}
