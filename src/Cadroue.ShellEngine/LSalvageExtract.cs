using System.Globalization;

using Cadroue.Application;
using Cadroue.Core;

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

        string lSalvageFolder = LEncodingPath.LEncodingFolderRead(
            lSalvageItem.LWorkOutput,
            lSalvageItem.LWorkSourcePath);
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

        string lSalvageTemp = LSalvageTemp.LSalvageTempResolve(lSalvageOutputPath);
        string lSalvageProgram = LToolEngine.LToolFfmpegSeam();
        LEmployerResult lSalvageResult;
        try
        {
            lSalvageResult = await LEmployerEngine.LEmployerRunSeam(
                lSalvageProgram,
                string.Empty,
                null,
                null,
                null,
                LSalvageArgumentBuild(lSalvageSource, lSalvageTemp, lSalvageOrigin, lSalvageLength),
                lSalvageToken,
                null,
                static _ => { },
                static _ => { }).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await LSalvageTemp.LSalvageTempClear(lSalvageTemp).ConfigureAwait(false);
            throw;
        }

        if (lSalvageResult.LEmployerExit == 0
            && File.Exists(lSalvageTemp)
            && new FileInfo(lSalvageTemp).Length > 0
            && await LScout.LScoutMediaRead(lSalvageTemp, lSalvageToken).ConfigureAwait(false) is not null)
        {
            return await LSalvageOutputPlace(
                lSalvageSource, lSalvageTemp, lSalvageOutputPath, lSalvageOrigin, lSalvageSpan.LSalvageSpanLimit)
                .ConfigureAwait(false);
        }

        LRunnerTrace.LRunnerRecord(
            $"Salvage discarded an unreadable span for '{Path.GetFileName(lSalvageOutputPath)}' " +
            $"(exit {lSalvageResult.LEmployerExit})");
        await LSalvageTemp.LSalvageTempClear(lSalvageTemp).ConfigureAwait(false);
        return false;
    }

    private static async Task<bool> LSalvageOutputPlace(
        string lSalvageSource,
        string lSalvageTemp,
        string lSalvageOutputPath,
        TimeSpan lSalvageOrigin,
        TimeSpan lSalvageLimit)
    {
        try
        {
            File.Move(lSalvageTemp, lSalvageOutputPath, true);
            LRunnerTrace.LRunnerRecord(
                $"Salvaged '{Path.GetFileName(lSalvageOutputPath)}' "
                + $"[{lSalvageOrigin:hh\\:mm\\:ss\\.fff}-{lSalvageLimit:hh\\:mm\\:ss\\.fff}] "
                + $"from '{Path.GetFileName(lSalvageSource)}'");
            return true;
        }
        catch (Exception lSalvageException) when (lSalvageException is IOException or UnauthorizedAccessException)
        {
            LRunnerTrace.LRunnerRecord(
                $"Salvage could not place the recovered output '{Path.GetFileName(lSalvageOutputPath)}'",
                lSalvageException);
            await LSalvageTemp.LSalvageTempClear(lSalvageTemp).ConfigureAwait(false);
            return false;
        }
    }

    private static string LSalvageArgumentBuild(
        string lSalvageSource, string lSalvageTemp, TimeSpan lSalvageOrigin, TimeSpan lSalvageLength)
    {
        return "-hide_banner -nostdin -y -err_detect ignore_err"
            + $" -ss {LSalvageSecondsFormat(lSalvageOrigin)}"
            + $" -i {LEncode.LEncodeFormat(lSalvageSource)}"
            + $" -t {LSalvageSecondsFormat(lSalvageLength)}"
            + " -map 0 -c copy -avoid_negative_ts make_zero -ignore_unknown"
            + $" {LEncode.LEncodeFormat(lSalvageTemp)}";
    }

    private static string LSalvageSecondsFormat(TimeSpan lSalvageTime) =>
        lSalvageTime.TotalSeconds.ToString("0.#######", CultureInfo.InvariantCulture);
}
