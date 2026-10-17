using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Cadroue.Core;

internal static class LClassifierPattern
{
    private const int lClassifierPatternLimit = 64;
    private static readonly Dictionary<string, Regex?> lClassifierPatternCache = new(StringComparer.Ordinal);

    public static TimeSpan LClassifierPatternTimeout { get; } = TimeSpan.FromMilliseconds(250);

    public static Regex? LClassifierPatternRead(string lClassifierPattern, out string? lClassifierFault)
    {
        lClassifierFault = null;
        lock (lClassifierPatternCache)
        {
            if (lClassifierPatternCache.TryGetValue(lClassifierPattern, out Regex? lClassifierCached))
            {
                return lClassifierCached;
            }
        }

        Regex? lClassifierRegex = null;
        try
        {
            lClassifierRegex = new Regex(
                lClassifierPattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                LClassifierPatternTimeout);
        }
        catch (ArgumentException lClassifierError)
        {
            lClassifierFault = $"Funnel rule pattern is invalid and matches nothing: {lClassifierPattern} "
                + $"({lClassifierError.Message})";
        }

        lock (lClassifierPatternCache)
        {
            if (lClassifierPatternCache.Count >= lClassifierPatternLimit)
            {
                lClassifierPatternCache.Clear();
            }

            lClassifierPatternCache[lClassifierPattern] = lClassifierRegex;
        }

        return lClassifierRegex;
    }
}
