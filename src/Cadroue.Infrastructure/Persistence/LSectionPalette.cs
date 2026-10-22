using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed record LSectionEntry(string LSectionEntryName, string[] LSectionEntryColors, string? LSectionEntryPath);

public static class LSectionPalette
{
    public const string LSectionPaletteDefault = "Cadroue";

    private static readonly LSectionEntry[] lSectionNative =
    [
        new("Cadroue", [
            "#4A90D9", "#27AE60", "#E67E22", "#8E44AD", "#E74C3C",
            "#16A085", "#F1C40F", "#D6499A", "#34495E", "#7F8C8D"
        ], null),
        new("Muted", [
            "#5B7C99", "#6A9C78", "#C08552", "#8E7CA6", "#C0736B",
            "#5F9EA0", "#B5A25D", "#A9788F", "#4F5D6B", "#8C8579"
        ], null),
        new("Vivid", [
            "#2979FF", "#00C853", "#FF9100", "#AA00FF", "#FF1744",
            "#00BFA5", "#FFD600", "#F50057", "#00B0FF", "#64DD17"
        ], null),
        new("Contrast", [
            "#332288", "#88CCEE", "#44AA99", "#117733", "#999933",
            "#DDCC77", "#CC6677", "#882255", "#AA4499", "#DDDDDD"
        ], null),
        new("Petroff", [
            "#707480", "#FFA90D", "#832DB5", "#E76300", "#92DADD",
            "#A96B59", "#3F90DA", "#B8AB6F", "#BC1E00", "#94A4A2"
        ], null),
        new("Observable", [
            "#9498A0", "#EFB118", "#A463F2", "#3CA951", "#FF8AB7",
            "#6CC5B0", "#FF725C", "#4269D0", "#9C6B4E", "#97BBF5"
        ], null),
        new("Tableau", [
            "#9C755F", "#76B7B2", "#E15759", "#EDC948", "#B07AA1",
            "#F28E2B", "#4E79A7", "#FF9DA7", "#59A14F", "#BAB0AC"
        ], null)
    ];

    private static readonly List<LSectionEntry> lSectionLoaded = [];

    private static readonly HashSet<string> lSectionHidden = new(StringComparer.Ordinal);

    public static int LSectionActiveCount => LSectionActiveRead().LSectionEntryColors.Length;

    public static string? LSectionNameFind(string lPath) =>
        lSectionLoaded
            .FirstOrDefault(lEntry =>
                string.Equals(lEntry.LSectionEntryPath, lPath, StringComparison.OrdinalIgnoreCase))
            ?.LSectionEntryName;

    public static string[] LSectionHexRead(string lName) =>
        LSectionEntryFind(lName)?.LSectionEntryColors.ToArray() ?? [];

    public static LSectionImportResult LSectionPaletteImport(
        Func<string> lFolder,
        string lSourcePath,
        out string lTargetPath,
        out string? lLoadedName)
    {
        LSectionImportResult lResult =
            LSectionPaletteStore.LSectionPaletteImport(lFolder, lSourcePath, out lTargetPath);
        if (lResult == LSectionImportResult.LSectionImportCopied)
        {
            LSectionRegistryLoad(lFolder());
        }

        lLoadedName = lResult == LSectionImportResult.LSectionImportCopied
            ? LSectionNameFind(lTargetPath)
            : null;
        return lResult;
    }

    public static string LSectionColorRead(int lColorIndex)
    {
        string[] lColors = LSectionActiveRead().LSectionEntryColors;
        return lColors[((lColorIndex % lColors.Length) + lColors.Length) % lColors.Length];
    }

    public static void LSectionRegistryLoad() => LSectionRegistryLoad(LDepot.LDepotPaletteRead());

    public static void LSectionRegistryLoad(string lFolder)
    {
        lSectionHidden.Clear();
        foreach (string lName in LSectionPaletteStore.LSectionHiddenLoad(lFolder))
        {
            if (!string.Equals(lName, LSectionPaletteDefault, StringComparison.Ordinal))
            {
                lSectionHidden.Add(lName);
            }
        }

        lSectionLoaded.Clear();
        foreach (LSectionPaletteFile lFile in LSectionPaletteStore.LSectionPaletteLoad(lFolder))
        {
            string[] lColors = lFile.LSectionPaletteColors
                .Select(LSectionHexNormalize)
                .OfType<string>()
                .ToArray();
            if (lColors.Length == 0)
            {
                continue;
            }

            lSectionLoaded.Add(new LSectionEntry(
                LSectionSpareResolve(lFile.LSectionPaletteName), lColors, lFile.LSectionPalettePath));
        }
    }

    public static IReadOnlyList<LSectionEntry> LSectionEntriesRead(string lFolder)
    {
        LSectionRegistryLoad(lFolder);
        return LSectionEntriesRead().ToArray();
    }

    public static string? LSectionHexNormalize(string lHex)
    {
        string lText = lHex.Trim();
        if (lText.Length is not (7 or 9) || lText[0] != '#' || !lText[1..].All(Uri.IsHexDigit))
        {
            return null;
        }

        return $"#{lText[^6..].ToUpperInvariant()}";
    }

    public static Func<string> LSectionPaletteSeam { get; set; } = () => LPreferenceState.LPreferencePaletteDefault;

    private static LSectionEntry LSectionActiveRead() =>
        LSectionEntryFind(LSectionPaletteSeam()) ?? lSectionNative[0];

    private static LSectionEntry? LSectionEntryFind(string lName) =>
        LSectionEntriesRead()
            .FirstOrDefault(lEntry => string.Equals(lEntry.LSectionEntryName, lName, StringComparison.Ordinal));

    private static IEnumerable<LSectionEntry> LSectionEntriesRead() =>
        lSectionNative
            .Where(lEntry => !lSectionHidden.Contains(lEntry.LSectionEntryName))
            .Concat(lSectionLoaded);

    private static string LSectionSpareResolve(string lName)
    {
        string lCandidate = lName;
        int lSuffix = 2;
        while (lSectionNative.Any(lEntry => lEntry.LSectionEntryName == lCandidate)
            || lSectionLoaded.Any(lEntry => lEntry.LSectionEntryName == lCandidate))
        {
            lCandidate = $"{lName} {lSuffix++}";
        }

        return lCandidate;
    }
}
