using System.Security.Cryptography;
using System.Text;

namespace Cadroue.Media;

public static class LSidecarStore
{
    public const string LSidecarRecordFolderName = "filerecord";

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
            lSidecar.Edit = lSidecarPrevious?.Edit;
            lSidecar.Audio = lSidecarPrevious?.Audio;

            return LSidecarWrite(lSidecarPath, lSidecar);
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
            return LSidecarRead(LSidecarPathRead(lSidecarSourcePath))?.Edit;
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
            lSidecar.Edit = lSidecarEdit;
            return LSidecarWrite(lSidecarPath, lSidecar);
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
            return LSidecarRead(LSidecarPathRead(lSidecarSourcePath))?.Audio;
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
            lSidecar.Audio = lSidecarAudio;
            return LSidecarWrite(lSidecarPath, lSidecar);
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
            return LSidecarRead(LSidecarPathRead(lSidecarSourcePath)) is { Source.DurationMilliseconds: > 0 } lSidecar
                ? TimeSpan.FromMilliseconds(lSidecar.Source.DurationMilliseconds)
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
            if (lSidecar is { Source.DurationMilliseconds: > 0 } lSidecarKnown)
            {
                return TimeSpan.FromMilliseconds(lSidecarKnown.Source.DurationMilliseconds);
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
            lSidecarTarget.Source.DurationMilliseconds = (long)Math.Round(lSidecarProbed.TotalMilliseconds);
            LSidecarWrite(lSidecarPath, lSidecarTarget);
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
            Source = new LSidecarSourceRecord
            {
                FileName = Path.GetFileName(lSidecarFullPath),
                RelativePath = string.IsNullOrWhiteSpace(lSidecarFolder)
                    ? string.Empty
                    : Path.GetRelativePath(lSidecarFolder, lSidecarFullPath),
                AbsolutePath = lSidecarFullPath,
                Length = lSidecarFile.Exists ? lSidecarFile.Length : 0,
                LastWriteUtcTicks = lSidecarFile.Exists ? lSidecarFile.LastWriteTimeUtc.Ticks : 0
            }
        };
    }

    private static bool LSidecarWrite(string lSidecarPath, LSidecar lSidecar)
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
