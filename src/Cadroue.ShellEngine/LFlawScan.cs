using System.Diagnostics;
using System.Text;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal static class LFlawScan
{
    internal static IReadOnlyList<LDossier> LFlawScanRun(LWorkItem lFlawItem, CancellationToken lFlawToken = default)
    {
        string lFlawSource = lFlawItem.LWorkSourcePath;
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
            (string lFlawMetaReport, _) = LFlawRunRead(
                LTool.LToolFfprobeRead(),
                $"-hide_banner -v error -show_streams -show_format -count_packets -i {LEncode.LEncodeFormat(lFlawSource)}",
                lFlawToken);
            (_, string lFlawIgnidxError) = LFlawRunRead(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -ignidx -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -",
                lFlawToken);
            (_, string lFlawSeekError) = LFlawRunRead(
                LTool.LToolFfmpegRead(),
                $"-hide_banner -nostdin -v error -sseof -3 -i {LEncode.LEncodeFormat(lFlawSource)} -map 0 -c copy -f null -",
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

            var lFlawDossiers = new List<LDossier>();
            if (LFlawMux.LFlawContainerResolve(lFlawProbeError, lFlawCopyError) is { } lFlawContainer)
            {
                lFlawDossiers.Add(lFlawContainer with { LDossierKind = LFlawKind.LFlawKindContainer });
            }

            if (LFlawMux.LFlawTruncationResolve(lFlawProbeError, lFlawCopyError) is { } lFlawTruncation)
            {
                lFlawDossiers.Add(lFlawTruncation with { LDossierKind = LFlawKind.LFlawKindTruncation });
            }

            if (LFlawMux.LFlawTransportResolve(lFlawMetaReport, lFlawCopyError) is { } lFlawTransport)
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

            if (LFlawCoded.LFlawCodedResolve(lFlawCodedError) is { } lFlawCoded)
            {
                lFlawDossiers.Add(lFlawCoded with { LDossierKind = LFlawKind.LFlawKindCoded });
            }

            if (LFlawFfvone.LFlawFfvoneResolve(lFlawMetaReport, lFlawCrcError) is { } lFlawFfvone)
            {
                lFlawDossiers.Add(lFlawFfvone with { LDossierKind = LFlawKind.LFlawKindFfvone });
            }

            return lFlawDossiers;
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
