using Cadroue.Core;

namespace Cadroue.Application;

public static class LConvert
{
    public static IReadOnlyList<LWorkItem> LConvertItemsCreate(
        LWorkPriority lWorkPriority,
        LConvertWorkDescription lConvertWorkDescription,
        string lConvertTab,
        Action<string> lErrorLog,
        Func<string, TimeSpan> lDurationRead)
    {
        IReadOnlyList<string> lConvertSourcePaths = lConvertWorkDescription.LConvertSourcePaths;
        if (lConvertSourcePaths.Count == 0)
        {
            lErrorLog("Convert not queued: the file list is empty");
            return Array.Empty<LWorkItem>();
        }

        LEncoding lConvertOutput = lConvertWorkDescription.LConvertOutput;
        var lConvertWorkItems = new List<LWorkItem>();
        var lConvertTakenPaths = new HashSet<string>(StringComparer.Ordinal);
        DateTimeOffset lConvertStamp = DateTimeOffset.Now;
        Guid lConvertLooseBatch = LGate.LGateBatchCreate();

        foreach (string lConvertSourcePath in lConvertSourcePaths)
        {
            Guid lConvertBatch = lConvertWorkDescription.LConvertRelays is { } lConvertRelayMap
                && lConvertRelayMap.TryGetValue(lConvertSourcePath, out Guid lConvertRelay)
                && lConvertRelay != Guid.Empty
                ? lConvertRelay
                : lConvertLooseBatch;

            LWorkMedia? lConvertMedia = null;
            if (lConvertWorkDescription.LConvertMedia is { } lConvertMap)
            {
                lConvertMap.TryGetValue(lConvertSourcePath, out lConvertMedia);
            }

            TimeSpan lConvertDuration = lConvertMedia?.LWorkMediaDuration
                ?? lDurationRead(lConvertSourcePath);

            string lConvertFolder = LEncodingPath.LEncodingFolderRead(lConvertOutput, lConvertSourcePath);
            string lConvertOutputName = LConvertName.LConvertNameCreate(
                lConvertOutput,
                lConvertSourcePath,
                lConvertFolder,
                lConvertDuration,
                lConvertStamp,
                lConvertTakenPaths);

            lConvertWorkItems.Add(new LWorkItem(
                lConvertBatch,
                LWorkKind.LWorkKindConvert,
                lWorkPriority,
                lConvertSourcePath,
                TimeSpan.Zero,
                lConvertDuration,
                lConvertOutputName,
                Path.Combine(lConvertFolder, lConvertOutputName),
                lConvertOutput)
            {
                LWorkSourceMedia = lConvertMedia,
                LWorkTab = lConvertTab
            });
        }

        return lConvertWorkItems;
    }
}
