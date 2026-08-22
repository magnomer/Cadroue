using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

public static partial class LSweep
{
    public static async Task<IReadOnlyList<(TimeSpan Start, TimeSpan End)>> LSweepScan(
        string lSweepSource, LDetectorBlank lSweepBlank, CancellationToken lSweepToken)
    {
        if (string.IsNullOrWhiteSpace(lSweepSource))
        {
            return Array.Empty<(TimeSpan, TimeSpan)>();
        }

        var lSweepLines = new List<string>();
        var lSweepEmployer = new LEmployer(LTool.LToolFfmpegRead());
        await lSweepEmployer.LEmployerRun(
            LSweepArgsFormat(lSweepSource, lSweepBlank),
            lSweepToken,
            _ => { },
            _ => { },
            lSweepLine => lSweepLines.Add(lSweepLine)).ConfigureAwait(false);

        return LSweepOutputParse(lSweepLines);
    }
}
