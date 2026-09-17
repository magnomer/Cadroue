using System.IO;
using System.Threading;

namespace Cadroue.Media;

public sealed record LMediaScanNotice(string LMediaScanFolder, string LMediaScanReason);

public sealed record LMediaScanResult(
    IReadOnlyList<string> LMediaScanPaths,
    IReadOnlyList<LMediaScanNotice> LMediaScanNotices);

public static partial class LMedia
{
    public static Task<LMediaScanResult> LMediaPathScan(
        IReadOnlyList<string> lMediaPaths,
        CancellationToken lMediaToken = default) =>
        lMediaPaths.Any(Directory.Exists)
            ? Task.Run(() => LMediaListScan(lMediaPaths, lMediaToken), lMediaToken)
            : Task.FromResult(LMediaListScan(lMediaPaths, lMediaToken));

    private static LMediaScanResult LMediaListScan(
        IReadOnlyList<string> lMediaPaths,
        CancellationToken lMediaToken)
    {
        var lMediaScanned = new List<string>();
        var lMediaNotices = new List<LMediaScanNotice>();
        foreach (string lMediaPath in lMediaPaths)
        {
            lMediaToken.ThrowIfCancellationRequested();
            if (File.Exists(lMediaPath) && LMediaCheck(lMediaPath))
            {
                lMediaScanned.Add(lMediaPath);
                continue;
            }

            if (!Directory.Exists(lMediaPath))
            {
                continue;
            }

            int lMediaStart = lMediaScanned.Count;
            LMediaFolderScan(lMediaPath, lMediaScanned, lMediaNotices, lMediaToken);
            lMediaScanned.Sort(lMediaStart, lMediaScanned.Count - lMediaStart, StringComparer.OrdinalIgnoreCase);
        }

        return new LMediaScanResult(lMediaScanned, lMediaNotices);
    }

    private static void LMediaFolderScan(
        string lMediaRoot,
        List<string> lMediaScanned,
        List<LMediaScanNotice> lMediaNotices,
        CancellationToken lMediaToken)
    {
        var lMediaPending = new Stack<string>();
        lMediaPending.Push(lMediaRoot);
        while (lMediaPending.Count > 0)
        {
            lMediaToken.ThrowIfCancellationRequested();
            string lMediaFolder = lMediaPending.Pop();
            try
            {
                var lMediaInfo = new DirectoryInfo(lMediaFolder);
                lMediaScanned.AddRange(lMediaInfo.EnumerateFiles()
                    .Select(lMediaFile => lMediaFile.FullName)
                    .Where(LMediaCheck));
                foreach (DirectoryInfo lMediaChild in lMediaInfo.EnumerateDirectories())
                {
                    if ((lMediaChild.Attributes & FileAttributes.ReparsePoint) == 0)
                    {
                        lMediaPending.Push(lMediaChild.FullName);
                    }
                }
            }
            catch (Exception lMediaError) when (lMediaError is IOException or UnauthorizedAccessException)
            {
                lMediaNotices.Add(new LMediaScanNotice(lMediaFolder, lMediaError.Message));
            }
        }
    }
}
