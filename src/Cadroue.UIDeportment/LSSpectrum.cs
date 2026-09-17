using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum LSSpectrumResult
{
    LSSpectrumResultLoaded,
    LSSpectrumResultInvalid,
    LSSpectrumResultReserved,
    LSSpectrumResultFailed
}

public sealed class LSSpectrum
{
    public const string LSSpectrumDefault = "Cadroue";

    private readonly Func<string> lsSpectrumFolder;
    private IReadOnlyList<string> lsSpectrumNames = [];
    private string lsSpectrumName;

    public event Action? LSSpectrumChange;

    public LSSpectrum(string lName)
        : this(lName, LDepot.LDepotPaletteRead)
    {
    }

    public LSSpectrum(string lName, Func<string> lFolder)
    {
        lsSpectrumName = lName;
        lsSpectrumFolder = lFolder;
    }

    public string LSSpectrumName => lsSpectrumName;

    public IReadOnlyList<string> LSSpectrumNames => lsSpectrumNames;

    public string LSSpectrumFolderRead() => lsSpectrumFolder();

    public static bool LSSpectrumFixedCheck(string lName) =>
        string.Equals(lName, LSSpectrumDefault, StringComparison.Ordinal);

    public void LSSpectrumNamesSet(IReadOnlyList<string> lNames)
    {
        lsSpectrumNames = lNames;
        if (!lNames.Contains(lsSpectrumName, StringComparer.Ordinal))
        {
            lsSpectrumName = LSSpectrumDefault;
        }

        LSSpectrumChange?.Invoke();
    }

    public void LSSpectrumSelect(string lName)
    {
        if (string.Equals(lsSpectrumName, lName, StringComparison.Ordinal))
        {
            return;
        }

        lsSpectrumName = lName;
        LSSpectrumChange?.Invoke();
    }

    public LSSpectrumResult LSSpectrumImport(string lSourcePath, out string lTargetPath)
    {
        lTargetPath = string.Empty;
        if (LSectionPalette.LSectionPaletteRead(lSourcePath) is not { } lFile)
        {
            return LSSpectrumResult.LSSpectrumResultInvalid;
        }

        return LSectionPalette.LSectionPaletteImport(
            lsSpectrumFolder(), lSourcePath, lFile.LSectionPaletteName, out lTargetPath) switch
        {
            LSectionImportResult.LSectionImportReserved => LSSpectrumResult.LSSpectrumResultReserved,
            LSectionImportResult.LSectionImportFailed => LSSpectrumResult.LSSpectrumResultFailed,
            _ => LSSpectrumResult.LSSpectrumResultLoaded
        };
    }

    public bool LSSpectrumSave(string lTargetPath, string lName, string[] lColors) =>
        LSectionPalette.LSectionPaletteSave(lTargetPath, lName, lColors);

    public bool LSSpectrumRemove(string lName, bool lNative, string? lPath)
    {
        if (LSSpectrumFixedCheck(lName))
        {
            return false;
        }

        if (lNative)
        {
            string lFolder = lsSpectrumFolder();
            var lHidden = new HashSet<string>(LSectionPalette.LSectionHiddenLoad(lFolder), StringComparer.Ordinal)
            {
                lName
            };
            LSectionPalette.LSectionHiddenSave(lFolder, lHidden.ToArray());
        }
        else if (lPath is null || !LSectionPalette.LSectionPaletteDelete(lPath))
        {
            return false;
        }

        if (string.Equals(lsSpectrumName, lName, StringComparison.Ordinal))
        {
            lsSpectrumName = LSSpectrumDefault;
        }

        return true;
    }
}
