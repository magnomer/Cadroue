using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

internal static class LSidecarCore
{
    internal static LSidecarCoreResult LSidecarCoreResolve(string lSidecarSourcePath, string lSidecarPreciousPath)
    {
        if (!File.Exists(lSidecarPreciousPath))
        {
            return new LSidecarCoreResult(LSidecarReadKind.LSidecarReadMissing, null, null);
        }

        string? lSidecarJson = LSidecarFile.LSidecarFileRead(lSidecarPreciousPath);
        if (lSidecarJson is null)
        {
            return new LSidecarCoreResult(LSidecarReadKind.LSidecarReadUnreadable, null, null);
        }

        if (LSidecarSerializer.LSidecarCoreParse(lSidecarJson) is not { } lSidecarCore)
        {
            return new LSidecarCoreResult(LSidecarReadKind.LSidecarReadMalformed, null, lSidecarJson);
        }

        return LSidecarSource.LSidecarSourceMatch(lSidecarSourcePath, lSidecarCore.LSidecarSource)
            ? new LSidecarCoreResult(LSidecarReadKind.LSidecarReadMatched, lSidecarCore, lSidecarJson)
            : new LSidecarCoreResult(LSidecarReadKind.LSidecarReadForeign, lSidecarCore, lSidecarJson);
    }

    internal static bool LSidecarCoreSave(string lSidecarSourcePath, Action<LSidecarCoreDocument> lSidecarMutate)
    {
        try
        {
            string lSidecarFullPath = Path.GetFullPath(lSidecarSourcePath);
            if (!File.Exists(lSidecarFullPath))
            {
                return false;
            }

            string lSidecarPreciousPath = LSidecarPath.LSidecarPathRead(lSidecarFullPath);
            using (LLatch.LLatchClaim(lSidecarPreciousPath))
            {
                LSidecarCoreResult lSidecarResult = LSidecarCoreResolve(lSidecarFullPath, lSidecarPreciousPath);
                switch (lSidecarResult.LSidecarCoreState)
                {
                    case LSidecarReadKind.LSidecarReadUnreadable:
                        return false;
                    case LSidecarReadKind.LSidecarReadMalformed
                        when !LSidecarFile.LSidecarBrokenMove(lSidecarPreciousPath):
                        return false;
                }

                bool lSidecarMatched = lSidecarResult.LSidecarCoreState == LSidecarReadKind.LSidecarReadMatched;
                string? lSidecarExistingJson = lSidecarMatched ? lSidecarResult.LSidecarCoreJson : null;
                LSidecarCacheStore.LSidecarCacheMove(lSidecarPreciousPath, lSidecarExistingJson);

                LSidecarCoreDocument lSidecarCore = lSidecarMatched
                    ? lSidecarResult.LSidecarCoreValue!
                    : new LSidecarCoreDocument();
                if (string.IsNullOrWhiteSpace(lSidecarCore.LSidecarSource.LSidecarPartialHash))
                {
                    lSidecarCore.LSidecarSource = LSidecarSourceCreate(
                        LKeyframeSourceIdentity.LKeyframeIdentityCreate(
                            lSidecarFullPath,
                            TimeSpan.FromMilliseconds(lSidecarCore.LSidecarSource.LSidecarDurationMilliseconds)),
                        lSidecarPreciousPath);
                }

                lSidecarMutate(lSidecarCore);
                return LSidecarFile.LSidecarFileSave(
                    lSidecarPreciousPath,
                    LSidecarSerializer.LSidecarCoreFormat(lSidecarCore, lSidecarExistingJson));
            }
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or TimeoutException)
        {
            return false;
        }
    }

    internal static LSidecarSourceEntry LSidecarSourceCreate(
        LKeyframeSourceIdentity lSidecarIdentity,
        string lSidecarPreciousPath)
    {
        string lSidecarFolder = Path.GetDirectoryName(Path.GetFullPath(lSidecarPreciousPath)) ?? string.Empty;
        string lSidecarSourcePath = lSidecarIdentity.LKeyframeSourcePath;
        return new LSidecarSourceEntry
        {
            LSidecarFileName = Path.GetFileName(lSidecarSourcePath),
            LSidecarRelativePath = LSidecarRelativeCreate(lSidecarFolder, lSidecarSourcePath),
            LSidecarAbsolutePath = lSidecarSourcePath,
            LSidecarLength = lSidecarIdentity.LKeyframeSourceLength,
            LSidecarWriteTicks = lSidecarIdentity.LKeyframeWriteTicks,
            LSidecarDurationMilliseconds = lSidecarIdentity.LKeyframeSourceDuration,
            LSidecarPartialHash = lSidecarIdentity.LKeyframePartialHash
        };
    }

    private static string LSidecarRelativeCreate(string lSidecarFolder, string lSidecarSourcePath)
    {
        if (string.IsNullOrWhiteSpace(lSidecarFolder))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetRelativePath(lSidecarFolder, lSidecarSourcePath);
        }
        catch (ArgumentException)
        {
            return string.Empty;
        }
    }
}
