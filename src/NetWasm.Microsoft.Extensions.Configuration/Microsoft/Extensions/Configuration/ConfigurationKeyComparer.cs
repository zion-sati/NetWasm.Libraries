using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration;

public class ConfigurationKeyComparer : IComparer<string>
{
    public static ConfigurationKeyComparer Instance { get; } = new();

    internal static Comparison<string> Comparison { get; } = Instance.Compare;

    public int Compare(string? x, string? y)
    {
        string[] left = (x ?? string.Empty).Split(':');
        string[] right = (y ?? string.Empty).Split(':');
        int count = Math.Min(left.Length, right.Length);
        for (int i = 0; i < count; i++)
        {
            string a = left[i];
            string b = right[i];
            bool aNumber = int.TryParse(a, out int ai);
            bool bNumber = int.TryParse(b, out int bi);
            int result = aNumber && bNumber
                ? ai.CompareTo(bi)
                : aNumber ? -1 : bNumber ? 1 : StringComparer.OrdinalIgnoreCase.Compare(a, b);
            if (result != 0) return result;
        }

        return left.Length.CompareTo(right.Length);
    }
}
