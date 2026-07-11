using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public static class CancellationTokenSourceExtention
{
    public static void SafeCancelAndDispose(this CancellationTokenSource cancellationTokenSource)
    {
        if (cancellationTokenSource?.IsCancellationRequested == false)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = default;
        }
    }
}
