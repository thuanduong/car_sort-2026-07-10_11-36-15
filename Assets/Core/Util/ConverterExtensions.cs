using System;

public static class ConverterExtensions
{
    public static int TryToConvertInt32(this string val, int defaultValue = 0)
    {
        try
        {
            int m = Convert.ToInt32(val);
            return m;
        }
        catch
        {
            return defaultValue;
        }
    }
}
