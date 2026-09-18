using System.Threading.Tasks;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LTrialSet
{
    private static HashSet<string>? lTrialSetVideo;
    private static HashSet<string>? lTrialSetAudio;
    private static Task? lTrialSetTask;

    public static void LTrialSetStart() =>
        LTrialSetStart(lEncoder => LTrial.LTrialRun(lEncoder, LTrialKind.LTrialKindVideo));

    public static Task LTrialSetStart(Func<string, Task<LTrialResult>> lTrial)
    {
        Task lTask = Task.Run(() => LTrialSetRun(lTrial));
        lTrialSetTask = lTask;
        return lTask;
    }

    private static async Task LTrialSetRun(Func<string, Task<LTrialResult>> lTrial)
    {
        var lAvailable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LRepertoireEncoder lCandidate in LRepertoireCatalog.LRepertoireEncodersRead())
        {
            foreach (string lEncoder in lCandidate.LRepertoireTokens)
            {
                if ((await lTrial(lEncoder)).LTrialSuccess)
                {
                    lAvailable.Add(lEncoder);
                }
            }
        }

        LTrialSetApply(lAvailable);
    }

    public static IReadOnlySet<string>? LTrialSetRead(LTrialKind lKind = LTrialKind.LTrialKindVideo) =>
        lKind == LTrialKind.LTrialKindAudio ? lTrialSetAudio : lTrialSetVideo;

    public static void LTrialSetApply(IEnumerable<string> lAvailable, LTrialKind lKind = LTrialKind.LTrialKindVideo)
    {
        var lSet = new HashSet<string>(lAvailable, StringComparer.OrdinalIgnoreCase);
        if (lSet.Count == 0)
        {
            return;
        }

        if (lKind == LTrialKind.LTrialKindAudio)
        {
            lTrialSetAudio = lSet;
        }
        else
        {
            lTrialSetVideo = lSet;
        }
    }

    public static void LTrialSetReset()
    {
        lTrialSetVideo = null;
        lTrialSetAudio = null;
        lTrialSetTask = null;
    }

    public static void LTrialSetDefer(Action lContinue)
    {
        if (lTrialSetTask is { IsCompleted: false } lTask)
        {
            lTask.ContinueWith(_ => lContinue(), TaskScheduler.Default);
        }
    }

    public static bool LTrialSetCheck(LRepertoireEncoder lCandidate) =>
        lTrialSetVideo is not { } lSet || lCandidate.LRepertoireTokens.Any(lSet.Contains);

    public static bool LTrialSetCheck(string lEncoder, LTrialKind lKind) =>
        LTrialSetRead(lKind) is not { } lSet || lSet.Contains(lEncoder);
}
