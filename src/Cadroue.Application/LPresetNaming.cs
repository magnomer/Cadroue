namespace Cadroue.Application;

// Preset names: the two native defaults and how they display, a free name in the catalogue, the file name an export
// takes, and the name an import keeps.
public static class LPresetNaming
{
    public const string LPresetSplitDefault = "Split (default)";
    public const string LPresetMergeDefault = "Merge (default)";

    public static string LPresetDisplayRead(string lPresetName) => lPresetName switch
    {
        LPresetSplitDefault => "Split",
        LPresetMergeDefault => "Merge",
        _ => lPresetName
    };

    public static string LPresetNameCreate(string lBaseName)
    {
        if (!LPresetTable.LPresetNames.Any(
                lName => string.Equals(lName, lBaseName, StringComparison.OrdinalIgnoreCase)))
        {
            return lBaseName;
        }

        for (int lIndex = 2; ; lIndex++)
        {
            string lCandidate = $"{lBaseName} {lIndex}";
            if (!LPresetTable.LPresetNames.Any(
                    lName => string.Equals(lName, lCandidate, StringComparison.OrdinalIgnoreCase)))
            {
                return lCandidate;
            }
        }
    }

    public static string LPresetFileFormat(string lPresetName)
    {
        char[] lInvalidCharacters = Path.GetInvalidFileNameChars();
        return new string(lPresetName
            .Trim()
            .Select(lCharacter => lInvalidCharacters.Contains(lCharacter) ? '_' : lCharacter)
            .ToArray());
    }

    public static string LPresetNameResolve(string lStoredName, string lFilePath)
    {
        string lName = lStoredName.Trim();
        return string.IsNullOrWhiteSpace(lName)
            ? Path.GetFileNameWithoutExtension(lFilePath).Trim()
            : lName;
    }
}
