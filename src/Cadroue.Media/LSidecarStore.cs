namespace Cadroue.Media;

public static class LSidecarStore
{
    public static string LSidecarPathRead(string lSidecarSourcePath) =>
        Path.ChangeExtension(Path.GetFullPath(lSidecarSourcePath), LSidecar.LSidecarExtension);

    public static bool LSidecarFileCheck(string lSidecarPath) =>
        string.Equals(Path.GetExtension(lSidecarPath), LSidecar.LSidecarExtension, StringComparison.OrdinalIgnoreCase);

    public static bool LSidecarSave(
        LKeyframeSourceIdentity lSidecarIdentity,
        IReadOnlyCollection<long> lSidecarKeyframeMilliseconds,
        IReadOnlyCollection<int> lSidecarScannedSpans,
        int lSidecarSpanGridMilliseconds,
        IReadOnlyList<LSidecarSectionRecord> lSidecarSections)
    {
        string lSidecarPath = LSidecarPathRead(lSidecarIdentity.LKeyframeSourcePath);
        try
        {
            LSidecar lSidecar = LSidecar.LSidecarCreate(
                lSidecarIdentity,
                lSidecarPath,
                lSidecarKeyframeMilliseconds,
                lSidecarScannedSpans,
                lSidecarSpanGridMilliseconds,
                lSidecarSections);

            string lSidecarTempPath = lSidecarPath + ".tmp";
            File.WriteAllText(lSidecarTempPath, lSidecar.LSidecarJsonCreate());
            File.Move(lSidecarTempPath, lSidecarPath, overwrite: true);
            return true;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static LSidecar? LSidecarLoad(LKeyframeSourceIdentity lSidecarIdentity)
    {
        LSidecar? lSidecar = LSidecarRead(LSidecarPathRead(lSidecarIdentity.LKeyframeSourcePath));
        return lSidecar is not null && lSidecar.LSidecarSourceMatch(lSidecarIdentity) ? lSidecar : null;
    }

    public static LSidecar? LSidecarRead(string lSidecarPath)
    {
        if (!File.Exists(lSidecarPath))
        {
            return null;
        }

        try
        {
            return LSidecar.LSidecarParse(File.ReadAllText(lSidecarPath));
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
