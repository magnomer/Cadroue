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

            var lFlawDossiers = new List<LDossier>();
            if (LFlaw.LFlawContainerResolve(lFlawProbeError, lFlawCopyError) is { } lFlawContainer)
            {
                lFlawDossiers.Add(lFlawContainer);
            }

            if (LFlaw.LFlawMetadataResolve(lFlawMetaReport) is { } lFlawMetadata)
            {
                lFlawDossiers.Add(lFlawMetadata);
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
