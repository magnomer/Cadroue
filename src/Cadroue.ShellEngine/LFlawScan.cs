using System.Diagnostics;
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

    public static IReadOnlyList<LDossier> LFlawScanRun(string lFlawSource, IReadOnlyCollection<LFlawKind> lFlawKinds, CancellationToken lFlawToken = default)
    {
        if (string.IsNullOrWhiteSpace(lFlawSource) || !File.Exists(lFlawSource))
        {
            return Array.Empty<LDossier>();
        }

        try
        {
            (_, string lFlawProbeError) = LFlawRunRead(
                LTool.LToolFfprobeRead(),
                $"-hide_banner -v error -show_error -show_format -i {LEncode.LEncodeFormat(lFlawSource)}",
                lFlawToken);
            (_, string lFlawCopyError) = LFlawRunRead(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -",
                lFlawToken);
            // Transport-stream continuity and PES faults are logged at warning level, not
            // error, so the transport probe reads one verbosity higher than the shared copy
            // pass; it feeds only the MPEG-TS-gated transport detector, never the others.
            (_, string lFlawTransportError) = LFlawRunRead(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v warning -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -",
                lFlawToken);
            (string lFlawMetaReport, _) = LFlawRunRead(
                LTool.LToolFfprobeRead(),
                $"-hide_banner -v error -show_streams -show_format -count_packets -i {LEncode.LEncodeFormat(lFlawSource)}",
                lFlawToken);
            (_, string lFlawIgnidxError) = LFlawRunRead(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -fflags +ignidx -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -",
                lFlawToken);
            // Seek to one second before end: a late target forces the demuxer to consult the
            // index, so broken random-access addressing surfaces here while a healthy file
            // stays silent. A near-start seek (a large -sseof on a short clip) would read
            // linearly and never touch the index.
            (_, string lFlawSeekError) = LFlawRunRead(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -sseof -1 -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -",
                lFlawToken);
            (_, string lFlawDecodeError) = LFlawRunRead(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -i {LEncode.LEncodeFormat(lFlawSource)} -an -map 0:v? -f null -",
                lFlawToken);
            (string lFlawPacketReport, _) = LFlawRunRead(
                LTool.LToolFfprobeRead(),
                $"-hide_banner -v error -show_packets -show_entries packet=stream_index,pts,dts,duration -i {LEncode.LEncodeFormat(lFlawSource)}",
                lFlawToken);
            (string lFlawChapterReport, _) = LFlawRunRead(
                LTool.LToolFfprobeRead(),
                $"-hide_banner -v error -show_chapters -i {LEncode.LEncodeFormat(lFlawSource)}",
                lFlawToken);
            (_, string lFlawSecondaryError) = LFlawRunRead(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -i {LEncode.LEncodeFormat(lFlawSource)} -map 0:s? -map 0:d? -c copy -f null -",
                lFlawToken);
            (_, string lFlawCodedError) = LFlawRunRead(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -err_detect +explode -i {LEncode.LEncodeFormat(lFlawSource)} -an -map 0:v? -f null -",
                lFlawToken);
            string lFlawCrcError = string.Empty;
            if (LFlawFfvone.LFlawFfvoneCheck(lFlawMetaReport))
            {
                (_, lFlawCrcError) = LFlawRunRead(
                    LTool.LToolFfmpegRead(),
                    $"-hide_banner -nostdin -v error -err_detect +crccheck -i {LEncode.LEncodeFormat(lFlawSource)} -an -map 0:v? -f null -",
                    lFlawToken);
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

            return LFlawKindsResolve(lFlawDossiers, lFlawKinds);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception lFlawException)
        {
            LRunner.LRunnerRecord($"Container structure could not be examined '{Path.GetFileName(lFlawSource)}'", lFlawException);
            return Array.Empty<LDossier>();
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

    private static (string Output, string Error) LFlawRunRead(string lFlawProgram, string lFlawArguments, CancellationToken lFlawToken)
    {
        var lFlawStartInfo = new ProcessStartInfo(lFlawProgram)
        {
            Arguments = lFlawArguments,
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

            Task<string> lFlawOutput = lFlawProcess.StandardOutput.ReadToEndAsync();
            string lFlawError = lFlawProcess.StandardError.ReadToEnd();
            lFlawProcess.WaitForExit();
            lFlawOutput.Wait(CancellationToken.None);
            lFlawToken.ThrowIfCancellationRequested();
            return (lFlawOutput.Result, lFlawError);
        }
        finally
        {
            if (lFlawProcess is not null && !lFlawProcess.HasExited)
                try { lFlawProcess.Kill(); } catch { }
            lFlawProcess?.Dispose();
        }
    }
}
