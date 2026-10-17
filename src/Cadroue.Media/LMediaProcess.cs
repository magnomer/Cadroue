using System.Text;
using System.Threading;

using Cadroue.Core;

namespace Cadroue.Media;

internal readonly record struct LMediaProcessResult(
    string LMediaProcessOutput,
    string LMediaProcessError,
    int LMediaProcessExit,
    bool LMediaProcessStalled);

internal static class LMediaProcess
{
    internal static readonly TimeSpan LMediaIdleLimit = TimeSpan.FromSeconds(30);

    internal static async Task<LMediaProcessResult> LMediaProcessRun(
        string lMediaProgram,
        IReadOnlyList<string> lMediaArguments,
        CancellationToken lMediaToken,
        LCustodyFamily? lMediaFamily = null,
        int? lMediaLevel = null)
    {
        var lMediaOutput = new StringBuilder();
        var lMediaEmployer = new LEmployer(lMediaProgram)
        {
            LEmployerFamily = lMediaFamily,
            LEmployerLevel = lMediaLevel,
            LEmployerIdleLimit = LMediaIdleLimit
        };
        LEmployerResult lMediaResult = await lMediaEmployer
            .LEmployerRun(lMediaArguments, lMediaToken, lMediaLine => lMediaOutput.Append(lMediaLine).Append('\n'))
            .ConfigureAwait(false);
        return new LMediaProcessResult(
            lMediaOutput.ToString(),
            lMediaResult.LEmployerError,
            lMediaResult.LEmployerStalled ? -1 : lMediaResult.LEmployerExit,
            lMediaResult.LEmployerStalled);
    }
}
