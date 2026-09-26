using System.Text;
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

        await LMedia.LMediaScanClaim(lToken).ConfigureAwait(false);
        try
        {
            return await LTrialProcessRun(lFfmpeg, lEncoder, lKind, lToken).ConfigureAwait(false);
        }
        finally
        {
            LMedia.LMediaScanRelease();
        }
    }

    private static async Task<LTrialResult> LTrialProcessRun(
        string lFfmpeg, string lEncoder, LTrialKind lKind, CancellationToken lToken)
    {
        var lOutput = new StringBuilder();
        using var lTimeout = CancellationTokenSource.CreateLinkedTokenSource(lToken);
        lTimeout.CancelAfter(TimeSpan.FromSeconds(LTrialTimeoutSeconds));
        try
        {
            LEmployerResult lResult = await new LEmployer(lFfmpeg).LEmployerRun(
                LTrialArgumentsRead(lEncoder, lKind),
                lTimeout.Token,
                _ => { },
                lLine => lOutput.AppendLine(lLine),
                _ => { }).ConfigureAwait(false);
            string lMessage = LTrialMessageShorten(lResult.LEmployerError, lOutput.ToString());
            return new LTrialResult(lResult.LEmployerExit == 0, $"exit {lResult.LEmployerExit}{lMessage}");
        }
        catch (OperationCanceledException) when (!lToken.IsCancellationRequested)
        {
            return new LTrialResult(false, $"timeout after {LTrialTimeoutSeconds}s");
        }
        catch (Exception lException) when (lException is not OperationCanceledException)
        {
            return new LTrialResult(false, lException.Message);
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
