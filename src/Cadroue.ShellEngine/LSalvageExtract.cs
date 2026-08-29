using System.Globalization;
using System.IO;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal static class LSalvageExtract
{
    internal static async Task<IReadOnlyList<string>> LSalvageExtractRun(
        LWorkItem lSalvageItem,
        string lSalvageInputPath,
        IReadOnlyList<LSalvageSpan> lSalvageSpans,
        CancellationToken lSalvageToken)
    {
        IReadOnlyList<LSalvageOutput> lSalvagePlan = LSalvage.LSalvagePlanCreate(
            lSalvageSpans,
            lSalvageItem.LWorkFixPlan.LWorkFixSalvage.LWorkSalvageMode,
            lSalvageItem.LWorkSourcePath,
            lSalvageItem.LWorkOutput);
        if (lSalvagePlan.Count == 0)
        {
            return Array.Empty<string>();
        }

        string lSalvageFolder = lSalvageItem.LWorkOutput.LEncodingFolderRead(lSalvageItem.LWorkSourcePath);
        if (!string.IsNullOrWhiteSpace(lSalvageFolder))
        {
            Directory.CreateDirectory(lSalvageFolder);
        }

        var lSalvageDelivered = new List<string>(lSalvagePlan.Count);
        foreach (LSalvageOutput lSalvageOutput in lSalvagePlan)
        {
            lSalvageToken.ThrowIfCancellationRequested();
            string lSalvagePath = Path.Combine(lSalvageFolder, lSalvageOutput.LSalvageOutputName);
            if (await LSalvageEntryRun(
                lSalvageInputPath, lSalvagePath, lSalvageOutput.LSalvageOutputSpan, lSalvageToken)
                .ConfigureAwait(false))
            {
                lSalvageDelivered.Add(lSalvagePath);
            }
        }

        return lSalvageDelivered;
    }

    private static async Task<bool> LSalvageEntryRun(
        string lSalvageSource, string lSalvageOutputPath, LSalvageSpan lSalvageSpan, CancellationToken lSalvageToken)
    {
        TimeSpan lSalvageOrigin = lSalvageSpan.LSalvageSpanOrigin < TimeSpan.Zero
            ? TimeSpan.Zero
            : lSalvageSpan.LSalvageSpanOrigin;
        TimeSpan lSalvageLength = lSalvageSpan.LSalvageSpanLimit - lSalvageOrigin;
        if (lSalvageLength <= TimeSpan.Zero)
        {
            return false;
        }

        string lSalvageTemp = LSalvageTempResolve(lSalvageOutputPath);
        var lSalvageEmployer = new LEmployer(LTool.LToolFfmpegRead());
        LEmployerResult lSalvageResult;
        try
        {
            lSalvageResult = await lSalvageEmployer.LEmployerRun(
                LSalvageArgumentBuild(lSalvageSource, lSalvageTemp, lSalvageOrigin, lSalvageLength),
                lSalvageToken,
                static _ => { },
                static _ => { },
                static _ => { }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            LSalvageTempClear(lSalvageTemp);
            throw;
        }

        // Fail safe: keep only a span that extracted cleanly and re-probes as real
        // media. Anything else is deleted so no partial or corrupt file is left behind.
        if (lSalvageResult.LEmployerExit == 0
            && File.Exists(lSalvageTemp)
            && new FileInfo(lSalvageTemp).Length > 0
            && LScout.LScoutMediaRead(lSalvageTemp, lSalvageToken) is not null)
        {
            try
            {
                File.Move(lSalvageTemp, lSalvageOutputPath, true);
                LRunner.LRunnerRecord(
                    $"Salvaged '{Path.GetFileName(lSalvageOutputPath)}' "
                    + $"[{lSalvageOrigin:hh\\:mm\\:ss\\.fff}-{lSalvageSpan.LSalvageSpanLimit:hh\\:mm\\:ss\\.fff}] "
                    + $"from '{Path.GetFileName(lSalvageSource)}'");
                return true;
            }
            catch (Exception lSalvageException) when (lSalvageException is IOException or UnauthorizedAccessException)
            {
                LRunner.LRunnerRecord(
                    $"Salvage could not place the recovered output '{Path.GetFileName(lSalvageOutputPath)}'", lSalvageException);
                LSalvageTempClear(lSalvageTemp);
                return false;
            }
        }

        LRunner.LRunnerRecord(
            $"Salvage discarded an unreadable span for '{Path.GetFileName(lSalvageOutputPath)}' (exit {lSalvageResult.LEmployerExit})");
        LSalvageTempClear(lSalvageTemp);
        return false;
    }

    private static string LSalvageArgumentBuild(
        string lSalvageSource, string lSalvageTemp, TimeSpan lSalvageOrigin, TimeSpan lSalvageLength)
    {
        // Careful stream copy of one decodable span, keeping the source container and
        // stream layout like the Fix copy stage; error tolerance lets the demuxer read
        // past the surrounding damage. Input seeking avoids decoding the broken file.
        return "-hide_banner -nostdin -y -err_detect ignore_err"
            + $" -ss {LSalvageSecondsFormat(lSalvageOrigin)}"
            + $" -i {LEncode.LEncodeFormat(lSalvageSource)}"
            + $" -t {LSalvageSecondsFormat(lSalvageLength)}"
            + " -map 0 -c copy -avoid_negative_ts make_zero -ignore_unknown"
            + $" {LEncode.LEncodeFormat(lSalvageTemp)}";
    }

    private static string LSalvageSecondsFormat(TimeSpan lSalvageTime) =>
        lSalvageTime.TotalSeconds.ToString("0.#######", CultureInfo.InvariantCulture);

    private static string LSalvageTempResolve(string lSalvageOutputPath)
    {
        string lSalvageFolder = Path.GetDirectoryName(lSalvageOutputPath) ?? string.Empty;
        string lSalvageStem = Path.GetFileNameWithoutExtension(lSalvageOutputPath);
        string lSalvageExtension = Path.GetExtension(lSalvageOutputPath);

        for (int lSalvageIndex = 0; ; lSalvageIndex++)
        {
            string lSalvageName = lSalvageIndex == 0
                ? $"{lSalvageStem}.cadsalvage{lSalvageExtension}"
                : $"{lSalvageStem}.cadsalvage ({lSalvageIndex + 1}){lSalvageExtension}";
            string lSalvageCandidate = Path.Combine(lSalvageFolder, lSalvageName);
            if (!File.Exists(lSalvageCandidate))
            {
                return lSalvageCandidate;
            }
        }
    }

    private static void LSalvageTempClear(string lSalvageTemp)
    {
        for (int lSalvageAttempt = 0; lSalvageAttempt < 5; lSalvageAttempt++)
        {
            try
            {
                if (!File.Exists(lSalvageTemp))
                {
                    return;
                }

                File.Delete(lSalvageTemp);
                return;
            }
            catch (Exception lSalvageException) when (lSalvageException is IOException or UnauthorizedAccessException)
            {
                System.Threading.Thread.Sleep(200);
            }
        }

        LRunner.LRunnerRecord($"Could not delete the salvage temporary '{lSalvageTemp}'; it may remain on disk.", null);
    }
}
