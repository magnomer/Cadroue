using System.Diagnostics;
using System.Threading.Tasks;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public enum LTrialKind
{
    LTrialKindVideo,
    LTrialKindAudio
}

public sealed record LTrialResult(bool LTrialSuccess, string LTrialMessage);

public static class LTrial
{
    private const int LTrialTimeoutSeconds = 6;

    public static Task<LTrialResult> LTrialRun(string lEncoder, LTrialKind lKind, CancellationToken lToken = default) =>
        Task.Run(() => LTrialLaneRun(lEncoder, lKind, lToken), CancellationToken.None);

    private static async Task<LTrialResult> LTrialLaneRun(string lEncoder, LTrialKind lKind, CancellationToken lToken)
    {
        lToken.ThrowIfCancellationRequested();
        string lFfmpeg = LTool.LToolFfmpegRead();
        if (string.IsNullOrWhiteSpace(lFfmpeg))
        {
            return new LTrialResult(false, "ffmpeg not resolved");
        }

        LMedia.LMediaScanClaim(lToken);
        try
        {
            return await LTrialProcessRun(lFfmpeg, lEncoder, lKind, lToken);
        }
        finally
        {
            LMedia.LMediaScanRelease();
        }
    }

    private static async Task<LTrialResult> LTrialProcessRun(
        string lFfmpeg, string lEncoder, LTrialKind lKind, CancellationToken lToken)
    {
        using var lProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = lFfmpeg,
                Arguments = LTrialArgumentsRead(lEncoder, lKind),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        try
        {
            lProcess.Start();
            LCustody.LCustodyAttach(lProcess);
            Task<string> lErrorTask = lProcess.StandardError.ReadToEndAsync();
            Task<string> lOutputTask = lProcess.StandardOutput.ReadToEndAsync();
            using var lTimeout = CancellationTokenSource.CreateLinkedTokenSource(lToken);
            lTimeout.CancelAfter(TimeSpan.FromSeconds(LTrialTimeoutSeconds));
            try
            {
                await lProcess.WaitForExitAsync(lTimeout.Token);
            }
            catch (OperationCanceledException)
            {
                LTrialProcessInterrupt(lProcess);
                lToken.ThrowIfCancellationRequested();
                return new LTrialResult(false, $"timeout after {LTrialTimeoutSeconds}s");
            }

            string lMessage = LTrialMessageShorten(await lErrorTask, await lOutputTask);
            return new LTrialResult(lProcess.ExitCode == 0, $"exit {lProcess.ExitCode}{lMessage}");
        }
        catch (Exception lException) when (lException is not OperationCanceledException)
        {
            return new LTrialResult(false, lException.Message);
        }
    }

    private static void LTrialProcessInterrupt(Process lProcess)
    {
        try
        {
            lProcess.Kill(true);
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static string LTrialArgumentsRead(string lEncoder, LTrialKind lKind) => lKind switch
    {
        LTrialKind.LTrialKindAudio =>
            $"-hide_banner -loglevel error -f lavfi -i anullsrc=r=48000:cl=stereo -t 0.1 -vn -c:a {lEncoder} -f null -",
        _ =>
            "-hide_banner -loglevel error -f lavfi -i testsrc2=size=320x240:rate=1 -frames:v 1 " +
            $"-an -c:v {lEncoder} -f null -"
    };

    private static string LTrialMessageShorten(string lError, string lOutput)
    {
        string lMessage = string.IsNullOrWhiteSpace(lError) ? lOutput : lError;
        lMessage = lMessage.Replace("\r", " ").Replace("\n", " ").Trim();
        return string.IsNullOrWhiteSpace(lMessage) ? string.Empty : $": {lMessage[..Math.Min(500, lMessage.Length)]}";
    }
}
