using System.IO;
using System.Text;
using System.Threading.Tasks;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static partial class LInventory
{
    private const int LInventoryTimeout = 20000;

    private static async Task<LInventoryProcess> LInventoryProcessRead(params string[] lInventoryArguments)
    {
        try
        {
            var lInventoryOutput = new StringBuilder();
            using var lInventoryLimit = new CancellationTokenSource(LInventoryTimeout);
            var lInventoryEmployer = new LEmployer(LTool.LToolFfmpegRead()) { LEmployerBackground = true };
            LEmployerResult lInventoryResult = await lInventoryEmployer.LEmployerRun(
                ["-hide_banner", .. lInventoryArguments],
                lInventoryLimit.Token,
                lInventoryLine => lInventoryOutput.AppendLine(lInventoryLine)).ConfigureAwait(false);
            return new LInventoryProcess(
                lInventoryResult.LEmployerExit == 0, lInventoryOutput.ToString(), lInventoryResult.LEmployerError);
        }
        catch (Exception lInventoryException)
            when (lInventoryException is System.ComponentModel.Win32Exception
                or InvalidOperationException
                or OperationCanceledException
                or IOException)
        {
            return LInventoryProcess.LInventoryProcessFailure;
        }
    }

    private sealed record LInventoryProcess(
        bool LInventoryProcessSuccess,
        string LInventoryProcessOut,
        string LInventoryProcessError)
    {
        public static readonly LInventoryProcess LInventoryProcessFailure = new(false, string.Empty, string.Empty);
    }
}
