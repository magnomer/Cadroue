using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

public static class LFlawScan
{
    internal static IReadOnlyList<LDossier> LFlawScanRun(LWorkItem lFlawItem, CancellationToken lFlawToken = default)
    {
        return LFlawScanRun(lFlawItem.LWorkSourcePath, Array.Empty<LFlawKind>(), lFlawToken);
    }

    public static IReadOnlyList<LDossier> LFlawScanRun(
        string lFlawSource,
        IReadOnlyCollection<LFlawKind> lFlawKinds,
        CancellationToken lFlawToken = default,
        IProgress<double>? lFlawProgress = null)
    {
        // A source that is missing or unreadable cannot be diagnosed. Returning an empty
        // (defect-free) result here would be recorded by callers as an authoritative "clean"
        // verdict for every kind — a false negative that then suppresses any real scan. Fail
        // instead so no diagnosis record is written for a scan that never happened.
        if (string.IsNullOrWhiteSpace(lFlawSource) || !File.Exists(lFlawSource))
        {
            throw new FileNotFoundException(
                "Diagnosis source is missing or unreadable.", lFlawSource ?? string.Empty);
        }

        try
        {
            const int lFlawStageCount = 12;
            int lFlawStageIndex = 0;
            lFlawProgress?.Report(0);
            TimeSpan lFlawDuration;
            try
            {
                lFlawDuration = LMedia.LMediaFfprobeRead(lFlawSource, lFlawToken).LMediaInfoDuration;
            }
            catch (Exception lFlawDurationException) when (
                lFlawDurationException is InvalidOperationException
                    or System.ComponentModel.Win32Exception
                    or IOException)
            {
                lFlawDuration = TimeSpan.Zero;
            }

            (string Output, string Error) LFlawStageRun(string lFlawProgram, string lFlawArguments)
            {
                double lFlawStart = (double)lFlawStageIndex / lFlawStageCount;
                double lFlawEnd = (double)++lFlawStageIndex / lFlawStageCount;
                bool lFlawFfmpeg = string.Equals(
                    lFlawProgram,
                    LTool.LToolFfmpegRead(),
                    StringComparison.OrdinalIgnoreCase);
                (string Output, string Error) lFlawResult = LFlawRunRead(
                    lFlawProgram,
                    lFlawArguments,
                    lFlawToken,
                    lFlawProgress,
                    lFlawStart,
                    lFlawEnd,
                    lFlawDuration,
                    lFlawFfmpeg);
                lFlawProgress?.Report(lFlawEnd);
                return lFlawResult;
            }

            (_, string lFlawProbeError) = LFlawStageRun(
                LTool.LToolFfprobeRead(),
                $"-hide_banner -v error -show_error -show_format -i {LEncode.LEncodeFormat(lFlawSource)}");
            (_, string lFlawCopyError) = LFlawStageRun(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -");
            // Transport-stream continuity and PES faults are logged at warning level, not
            // error, so the transport probe reads one verbosity higher than the shared copy
            // pass; it feeds only the MPEG-TS-gated transport detector, never the others.
            (_, string lFlawTransportError) = LFlawStageRun(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v warning -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -");
            (string lFlawMetaReport, _) = LFlawStageRun(
                LTool.LToolFfprobeRead(),
                $"-hide_banner -v error -show_streams -show_format -count_packets -i {LEncode.LEncodeFormat(lFlawSource)}");
            (_, string lFlawIgnidxError) = LFlawStageRun(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -fflags +ignidx -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -");
            // Seek to one second before end: a late target forces the demuxer to consult the
            // index, so broken random-access addressing surfaces here while a healthy file
            // stays silent. A near-start seek (a large -sseof on a short clip) would read
            // linearly and never touch the index.
            (_, string lFlawSeekError) = LFlawStageRun(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -sseof -1 -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -");
            (_, string lFlawDecodeError) = LFlawStageRun(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -i {LEncode.LEncodeFormat(lFlawSource)} -an -map 0:v? -f null -");
            (string lFlawPacketReport, _) = LFlawStageRun(
                LTool.LToolFfprobeRead(),
                $"-hide_banner -v error -show_packets -show_entries packet=stream_index,pts,dts,duration -i {LEncode.LEncodeFormat(lFlawSource)}");
            (string lFlawChapterReport, _) = LFlawStageRun(
                LTool.LToolFfprobeRead(),
                $"-hide_banner -v error -show_chapters -i {LEncode.LEncodeFormat(lFlawSource)}");
            (_, string lFlawSecondaryError) = LFlawStageRun(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -i {LEncode.LEncodeFormat(lFlawSource)} -map 0:s? -map 0:d? -c copy -f null -");
            (_, string lFlawCodedError) = LFlawStageRun(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -err_detect +explode -i {LEncode.LEncodeFormat(lFlawSource)} -an -map 0:v? -f null -");
            string lFlawCrcError = string.Empty;
            if (LFlawFfvone.LFlawFfvoneCheck(lFlawMetaReport))
            {
                (_, lFlawCrcError) = LFlawStageRun(
                    LTool.LToolFfmpegRead(),
                    $"-hide_banner -nostdin -v error -err_detect +crccheck -i {LEncode.LEncodeFormat(lFlawSource)} -an -map 0:v? -f null -");
            }

            // A container the probe could open reports at least a format or one stream.
            // When it reports neither, the file never opened, and every structural and
            // per-stream probe below only echoes that one open failure. Emitting the
            // finalization defect alone keeps the diagnosis honest instead of scattering
            // the same failure across the container, coded and timing detectors.
            bool lFlawOpened = lFlawMetaReport.Contains("[FORMAT]", StringComparison.Ordinal)
                || lFlawMetaReport.Contains("[STREAM]", StringComparison.Ordinal);
            if (!lFlawOpened)
            {
                var lFlawUnopened = new List<LDossier>();
                if (LFlawMux.LFlawTruncationResolve(lFlawProbeError, lFlawCopyError) is { } lFlawFinal)
                {
                    lFlawUnopened.Add(lFlawFinal with { LDossierKind = LFlawKind.LFlawKindTruncation });
                }
                else if (LFlawMux.LFlawContainerResolve(lFlawProbeError, lFlawCopyError) is { } lFlawOpen)
                {
                    lFlawUnopened.Add(lFlawOpen with { LDossierKind = LFlawKind.LFlawKindContainer });
                }

                return LFlawKindsResolve(lFlawUnopened, lFlawKinds);
            }

            var lFlawDossiers = new List<LDossier>();
            if (LFlawMux.LFlawContainerResolve(lFlawProbeError, lFlawCopyError) is { } lFlawContainer)
            {
                lFlawDossiers.Add(lFlawContainer with { LDossierKind = LFlawKind.LFlawKindContainer });
            }

            if (LFlawMux.LFlawTruncationResolve(lFlawProbeError, lFlawCopyError) is { } lFlawTruncation)
            {
                lFlawDossiers.Add(lFlawTruncation with { LDossierKind = LFlawKind.LFlawKindTruncation });
            }

            if (LFlawMux.LFlawTransportResolve(lFlawMetaReport, lFlawTransportError) is { } lFlawTransport)
            {
                lFlawDossiers.Add(lFlawTransport with { LDossierKind = LFlawKind.LFlawKindTransport });
            }

            if (LFlawMux.LFlawMetadataResolve(lFlawMetaReport) is { } lFlawMetadata)
            {
                lFlawDossiers.Add(lFlawMetadata with { LDossierKind = LFlawKind.LFlawKindMetadata });
            }

            if (LFlawMux.LFlawIndexResolve(lFlawCopyError, lFlawIgnidxError, lFlawSeekError) is { } lFlawIndex)
            {
                lFlawDossiers.Add(lFlawIndex with { LDossierKind = LFlawKind.LFlawKindIndex });
            }

            if (LFlawStream.LFlawFramingResolve(lFlawCopyError, lFlawMetaReport) is { } lFlawFraming)
            {
                lFlawDossiers.Add(lFlawFraming with { LDossierKind = LFlawKind.LFlawKindFraming });
            }

            if (LFlawStream.LFlawConfigResolve(lFlawMetaReport, lFlawDecodeError) is { } lFlawConfig)
            {
                lFlawDossiers.Add(lFlawConfig with { LDossierKind = LFlawKind.LFlawKindConfig });
            }

            if (LFlawStream.LFlawTimingResolve(lFlawPacketReport) is { } lFlawTiming)
            {
                lFlawDossiers.Add(lFlawTiming with { LDossierKind = LFlawKind.LFlawKindTiming });
            }

            if (LFlawSecondary.LFlawSecondaryResolve(lFlawMetaReport, lFlawChapterReport, lFlawSecondaryError) is { } lFlawSecondary)
            {
                lFlawDossiers.Add(lFlawSecondary with { LDossierKind = LFlawKind.LFlawKindSecondary });
            }

            if (LFlawFfvone.LFlawFfvoneResolve(lFlawMetaReport, lFlawCrcError) is { } lFlawFfvone)
            {
                lFlawDossiers.Add(lFlawFfvone with { LDossierKind = LFlawKind.LFlawKindFfvone });
            }

            // Coded media is the last-resort, lossy re-encode item. The diagnostic decode
            // also fails whenever an upstream carriage defect — broken container, missing
            // finalization, transport faults, framing, codec configuration or an FFV1
            // integrity mismatch — corrupts the bitstream it reads, so that decode failure
            // is already explained by a losslessly repairable defect and must not escalate
            // this file to re-encode. Only decode damage that survives every carriage
            // diagnosis is a genuine coded defect.
            if (!lFlawDossiers.Any(lFlawDossier => LFlawCarriageCheck(lFlawDossier.LDossierKind))
                && LFlawCoded.LFlawCodedResolve(lFlawCodedError) is { } lFlawCoded)
            {
                lFlawDossiers.Add(lFlawCoded with { LDossierKind = LFlawKind.LFlawKindCoded });
            }

            lFlawProgress?.Report(1);
            return LFlawKindsResolve(lFlawDossiers, lFlawKinds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception lFlawException)
        {
            // A scan that could not complete must not be mistaken for a clean file. Returning
            // an empty result would let callers persist a false "no defect" record that then
            // blocks any future diagnosis of every kind. Surface the failure so the caller
            // reports it and writes nothing.
            LRunner.LRunnerRecord($"Container structure could not be examined '{Path.GetFileName(lFlawSource)}'", lFlawException);
            throw;
        }
    }

    private static bool LFlawCarriageCheck(LFlawKind lFlawKind) => lFlawKind switch
    {
        LFlawKind.LFlawKindContainer => true,
        LFlawKind.LFlawKindTruncation => true,
        LFlawKind.LFlawKindTransport => true,
        LFlawKind.LFlawKindFraming => true,
        LFlawKind.LFlawKindConfig => true,
        LFlawKind.LFlawKindFfvone => true,
        _ => false
    };

    internal static IReadOnlyList<LDossier> LFlawKindsResolve(
        IReadOnlyList<LDossier> lFlawDossiers,
        IReadOnlyCollection<LFlawKind> lFlawKinds)
    {
        if (lFlawKinds.Count == 0)
        {
            return lFlawDossiers;
        }

        var lFlawFiltered = new List<LDossier>(lFlawDossiers.Count);
        foreach (LDossier lFlawDossier in lFlawDossiers)
        {
            if (lFlawKinds.Contains(lFlawDossier.LDossierKind))
            {
                lFlawFiltered.Add(lFlawDossier);
            }
        }

        return lFlawFiltered;
    }

    private static (string Output, string Error) LFlawRunRead(
        string lFlawProgram,
        string lFlawArguments,
        CancellationToken lFlawToken,
        IProgress<double>? lFlawProgress,
        double lFlawStart,
        double lFlawEnd,
        TimeSpan lFlawDuration,
        bool lFlawFfmpeg)
    {
        bool lFlawProgressEnabled = lFlawFfmpeg && lFlawProgress is not null && lFlawDuration > TimeSpan.Zero;
        var lFlawStartInfo = new ProcessStartInfo(lFlawProgram)
        {
            Arguments = lFlawProgressEnabled
                ? "-progress pipe:1 -nostats " + lFlawArguments
                : lFlawArguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process? lFlawProcess = null;
        try
        {
            lFlawProcess = Process.Start(lFlawStartInfo);
            if (lFlawProcess is null)
            {
                return (string.Empty, string.Empty);
            }

            LCustody.LCustodyAttach(lFlawProcess);
            using CancellationTokenRegistration lFlawKill = lFlawToken.Register(
                static p => { try { ((Process)p!).Kill(); } catch { } }, lFlawProcess);

            Task<string> lFlawErrorTask = lFlawProcess.StandardError.ReadToEndAsync();
            string lFlawOutput;
            if (lFlawProgressEnabled)
            {
                var lFlawOutputBuilder = new StringBuilder();
                while (lFlawProcess.StandardOutput.ReadLine() is { } lFlawLine)
                {
                    lFlawOutputBuilder.AppendLine(lFlawLine);
                    LFlawProgressApply(
                        lFlawLine,
                        lFlawDuration,
                        lFlawStart,
                        lFlawEnd,
                        lFlawProgress!);
                }

                lFlawOutput = lFlawOutputBuilder.ToString();
            }
            else
            {
                lFlawOutput = lFlawProcess.StandardOutput.ReadToEnd();
            }

            lFlawProcess.WaitForExit();
            lFlawToken.ThrowIfCancellationRequested();
            return (lFlawOutput, lFlawErrorTask.GetAwaiter().GetResult());
        }
        finally
        {
            if (lFlawProcess is not null && !lFlawProcess.HasExited)
                try { lFlawProcess.Kill(); } catch { }
            lFlawProcess?.Dispose();
        }
    }

    internal static void LFlawProgressApply(
        string lFlawLine,
        TimeSpan lFlawDuration,
        double lFlawStart,
        double lFlawEnd,
        IProgress<double> lFlawProgress)
    {
        int lFlawSeparator = lFlawLine.IndexOf('=');
        if (lFlawSeparator <= 0
            || lFlawLine[..lFlawSeparator] is not ("out_time_us" or "out_time_ms")
            || !long.TryParse(
                lFlawLine[(lFlawSeparator + 1)..],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long lFlawMicroseconds)
            || lFlawDuration <= TimeSpan.Zero)
        {
            return;
        }

        double lFlawFraction = lFlawMicroseconds / 1_000_000d / lFlawDuration.TotalSeconds;
        lFlawProgress.Report(lFlawStart + (lFlawEnd - lFlawStart) * Math.Clamp(lFlawFraction, 0, 1));
    }
}
