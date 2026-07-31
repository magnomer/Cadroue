using System.Security.Cryptography;
using System.Text;

namespace Cadroue.Media;

public static class LSidecarStore
{
    public const string LSidecarRecordFolder = "filerecord";

    private static string? lSidecarRecordFolder;
    private static bool lSidecarRecordActive;

    public static void LSidecarFolderSet(string? lSidecarFolder, bool lSidecarActive)
    {
        lSidecarRecordFolder = string.IsNullOrWhiteSpace(lSidecarFolder) ? null : lSidecarFolder.Trim();
        lSidecarRecordActive = lSidecarActive && lSidecarRecordFolder is not null;
    }

    public static string LSidecarFolderRead() => lSidecarRecordFolder ?? string.Empty;

    public static bool LSidecarFolderCheck() => lSidecarRecordActive;

    public static string LSidecarPathRead(string lSidecarSourcePath) =>
        lSidecarRecordActive && lSidecarRecordFolder is { } lSidecarFolder
            ? Path.Combine(lSidecarFolder, LSidecarKeyCreate(lSidecarSourcePath) + LSidecar.LSidecarExtension)
            : Path.ChangeExtension(Path.GetFullPath(lSidecarSourcePath), LSidecar.LSidecarExtension);

    private static string LSidecarKeyCreate(string lSidecarSourcePath)
    {
        string lSidecarFullPath = Path.GetFullPath(lSidecarSourcePath).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(lSidecarFullPath)));
    }

    public static int LSidecarFolderClear()
    {
        if (lSidecarRecordFolder is not { } lSidecarFolder || !Directory.Exists(lSidecarFolder))
        {
            return 0;
        }

        int lSidecarRemoved = 0;
        foreach (string lSidecarFilePath in Directory
                     .EnumerateFiles(lSidecarFolder, "*" + LSidecar.LSidecarExtension)
                     .ToArray())
        {
            try
            {
                File.Delete(lSidecarFilePath);
                lSidecarRemoved++;
            }
            catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
            {
            }
        }

        return lSidecarRemoved;
    }

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
            LSidecar? lSidecarPrevious = LSidecarRead(lSidecarPath);
            lSidecar.LSidecarEdit = lSidecarPrevious?.LSidecarEdit;
            lSidecar.LSidecarAudio = lSidecarPrevious?.LSidecarAudio;
            lSidecar.LSidecarWaveform = lSidecarPrevious?.LSidecarWaveform;

            return LSidecarFileSave(lSidecarPath, lSidecar);
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static LSidecarEditRecord? LSidecarEditRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarRead(LSidecarPathRead(lSidecarSourcePath))?.LSidecarEdit;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarEditSave(string lSidecarSourcePath, LSidecarEditRecord? lSidecarEdit)
    {
        try
        {
            string lSidecarPath = LSidecarPathRead(lSidecarSourcePath);
            LSidecar lSidecar = LSidecarRead(lSidecarPath) ?? LSidecarStubCreate(lSidecarPath, lSidecarSourcePath);
            lSidecar.LSidecarEdit = lSidecarEdit;
            return LSidecarFileSave(lSidecarPath, lSidecar);
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    public static LSidecarAudioRecord? LSidecarAudioRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarRead(LSidecarPathRead(lSidecarSourcePath))?.LSidecarAudio;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarAudioSave(string lSidecarSourcePath, LSidecarAudioRecord? lSidecarAudio)
    {
        try
        {
            string lSidecarPath = LSidecarPathRead(lSidecarSourcePath);
            LSidecar lSidecar = LSidecarRead(lSidecarPath) ?? LSidecarStubCreate(lSidecarPath, lSidecarSourcePath);
            lSidecar.LSidecarAudio = lSidecarAudio;
            return LSidecarFileSave(lSidecarPath, lSidecar);
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    public static LSidecarWaveformRecord? LSidecarWaveformRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarRead(LSidecarPathRead(lSidecarSourcePath))?.LSidecarWaveform;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarWaveformSave(string lSidecarSourcePath, LSidecarWaveformRecord? lSidecarWaveform)
    {
        try
        {
            string lSidecarPath = LSidecarPathRead(lSidecarSourcePath);
            LSidecar lSidecar = LSidecarRead(lSidecarPath) ?? LSidecarStubCreate(lSidecarPath, lSidecarSourcePath);
            lSidecar.LSidecarWaveform = lSidecarWaveform;
            return LSidecarFileSave(lSidecarPath, lSidecar);
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    public static TimeSpan LSidecarDurationRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarRead(LSidecarPathRead(lSidecarSourcePath)) is { LSidecarSource.LSidecarDurationMilliseconds: > 0 } lSidecar
                ? TimeSpan.FromMilliseconds(lSidecar.LSidecarSource.LSidecarDurationMilliseconds)
                : TimeSpan.Zero;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return TimeSpan.Zero;
        }
    }

    public static TimeSpan LSidecarDurationResolve(string lSidecarSourcePath)
    {
        try
        {
            string lSidecarPath = LSidecarPathRead(lSidecarSourcePath);
            LSidecar? lSidecar = LSidecarRead(lSidecarPath);
            if (lSidecar is { LSidecarSource.LSidecarDurationMilliseconds: > 0 } lSidecarKnown)
            {
                return TimeSpan.FromMilliseconds(lSidecarKnown.LSidecarSource.LSidecarDurationMilliseconds);
            }

            TimeSpan lSidecarProbed;
            try
            {
                lSidecarProbed = LMediaInfo.LMediaFfprobeRead(lSidecarSourcePath).LMediaInfoDuration;
            }
            catch (Exception)
            {
                return TimeSpan.Zero;
            }

            if (lSidecarProbed <= TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            LSidecar lSidecarTarget = lSidecar ?? LSidecarStubCreate(lSidecarPath, lSidecarSourcePath);
            lSidecarTarget.LSidecarSource.LSidecarDurationMilliseconds = (long)Math.Round(lSidecarProbed.TotalMilliseconds);
            LSidecarFileSave(lSidecarPath, lSidecarTarget);
            return lSidecarProbed;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return TimeSpan.Zero;
        }
    }

    private static LSidecar LSidecarStubCreate(string lSidecarPath, string lSidecarSourcePath)
    {
        string lSidecarFullPath = Path.GetFullPath(lSidecarSourcePath);
        var lSidecarFile = new FileInfo(lSidecarFullPath);
        string lSidecarFolder = Path.GetDirectoryName(Path.GetFullPath(lSidecarPath)) ?? string.Empty;

        return new LSidecar
        {
            LSidecarSource = new LSidecarSourceRecord
            {
                LSidecarFileName = Path.GetFileName(lSidecarFullPath),
                LSidecarRelativePath = string.IsNullOrWhiteSpace(lSidecarFolder)
                    ? string.Empty
                    : Path.GetRelativePath(lSidecarFolder, lSidecarFullPath),
                LSidecarAbsolutePath = lSidecarFullPath,
                LSidecarLength = lSidecarFile.Exists ? lSidecarFile.Length : 0,
                LSidecarWriteTicks = lSidecarFile.Exists ? lSidecarFile.LastWriteTimeUtc.Ticks : 0
            }
        };
    }

    private static bool LSidecarFileSave(string lSidecarPath, LSidecar lSidecar)
    {
        if (Path.GetDirectoryName(lSidecarPath) is { Length: > 0 } lSidecarFolder)
        {
            Directory.CreateDirectory(lSidecarFolder);
        }

        string lSidecarTempPath = lSidecarPath + ".tmp";
        File.WriteAllText(lSidecarTempPath, lSidecar.LSidecarJsonCreate());
        File.Move(lSidecarTempPath, lSidecarPath, overwrite: true);
        return true;
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
