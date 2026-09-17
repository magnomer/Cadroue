using System.Threading.Tasks;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LTrialSet
{
    private static HashSet<string>? lTrialSetAvailable;
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

    public static IReadOnlySet<string>? LTrialSetRead() => lTrialSetAvailable;

    public static void LTrialSetApply(IEnumerable<string> lAvailable)
    {
        var lSet = new HashSet<string>(lAvailable, StringComparer.OrdinalIgnoreCase);
        if (lSet.Count > 0)
        {
            lTrialSetAvailable = lSet;
        }
    }

    public static void LTrialSetReset()
    {
        lTrialSetAvailable = null;
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
        lTrialSetAvailable is not { } lSet || lCandidate.LRepertoireTokens.Any(lSet.Contains);
}
