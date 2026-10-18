namespace Cadroue.Application;

public static class LLocalizationResource
{
    private const string LLocalizationResourcePrefix = "localization.";
    private const string LLocalizationResourceSuffix = ".json";

    public static Func<IEnumerable<string>>? LLocalizationNamesSeam { get; set; }

    public static Func<string, string?>? LLocalizationTextSeam { get; set; }

    public static string? LLocalizationResourceRead(string lLocalizationName) =>
        LLocalizationTextSeam?.Invoke(lLocalizationName);

    public static string? LLocalizationStreamRead(Stream? lLocalizationStream)
    {
        if (lLocalizationStream is null)
        {
            return null;
        }

        using var lLocalizationReader = new StreamReader(lLocalizationStream);
        return lLocalizationReader.ReadToEnd();
    }

    internal static IEnumerable<string> LLocalizationCodesRead() =>
        (LLocalizationNamesSeam?.Invoke() ?? Array.Empty<string>())
            .Where(lLocalizationName =>
                lLocalizationName.StartsWith(LLocalizationResourcePrefix, StringComparison.Ordinal)
                && lLocalizationName.EndsWith(LLocalizationResourceSuffix, StringComparison.Ordinal))
            .Select(lLocalizationName => lLocalizationName[
                LLocalizationResourcePrefix.Length..^LLocalizationResourceSuffix.Length])
            .Where(lLocalizationCode => !lLocalizationCode.Contains('.'))
            .OrderBy(lLocalizationCode => lLocalizationCode, StringComparer.OrdinalIgnoreCase);

    internal static LLocalizationCatalog? LLocalizationCatalogRead(string lLocalizationCode)
    {
        try
        {
            if (LLocalizationResourceRead(
                    LLocalizationResourcePrefix + lLocalizationCode + LLocalizationResourceSuffix)
                is not { } lLocalizationJson)
            {
                return null;
            }

            return LLocalizationCatalog.LLocalizationCatalogLoad(lLocalizationCode, lLocalizationJson);
        }
        catch (Exception lLocalizationException)
        {
            LLocalization.LLocalizationTraceRecord(
                $"Localization could not be loaded: {lLocalizationCode}",
                lLocalizationException);
            return null;
        }
    }
}
