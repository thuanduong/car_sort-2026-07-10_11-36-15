using System;
namespace ActionUtility
{

    public static class EmptyAction
    {
        public static readonly Action Instance = () => { };
    }

    public static class EmptyAction<T>
    {
        public static readonly Action<T> Instance = _ => { };
    }

    public static class EmptyAction<T1, T2>
    {
        public static readonly Action<T1, T2> Instance = (_, __) => { };
    }

    public static class EmptyAction<T1, T2, T3>
    {
        public static readonly Action<T1, T2, T3> Instance = (_, __, ___) => { };
    } 
    
    public static class EmptyFunc<T>
    {
        public static readonly Func<T> Instance = () => default(T);
    } 
}
