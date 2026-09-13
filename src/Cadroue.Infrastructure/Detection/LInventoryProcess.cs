using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static partial class LInventory
{
    private const int LInventoryTimeout = 20000;

    private static LInventoryProcess LInventoryProcessRead(params string[] lInventoryArguments)
    {
        try
        {
            var lInventoryStart = new ProcessStartInfo(LTool.LToolFfmpegRead())
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            lInventoryStart.ArgumentList.Add("-hide_banner");
            foreach (string lInventoryArgument in lInventoryArguments)
            {
                lInventoryStart.ArgumentList.Add(lInventoryArgument);
            }

            using var lInventoryProcess = Process.Start(lInventoryStart);
            if (lInventoryProcess is null)
            {
                return LInventoryProcess.LInventoryProcessFailure;
            }

            LCustody.LCustodyAttach(lInventoryProcess);
            Task<string> lInventoryOutputTask = lInventoryProcess.StandardOutput.ReadToEndAsync();
            Task<string> lInventoryErrorTask = lInventoryProcess.StandardError.ReadToEndAsync();
            if (!lInventoryProcess.WaitForExit(LInventoryTimeout))
            {
                LInventoryProcessInterrupt(lInventoryProcess);
                return LInventoryProcess.LInventoryProcessFailure;
            }

            string lInventoryError = lInventoryErrorTask.GetAwaiter().GetResult();
            string lInventoryOutput = lInventoryOutputTask.GetAwaiter().GetResult();
            return new LInventoryProcess(lInventoryProcess.ExitCode == 0, lInventoryOutput, lInventoryError);
        }
        catch (Exception lInventoryException)
            when (lInventoryException is System.ComponentModel.Win32Exception
                or InvalidOperationException
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

    private static void LInventoryProcessInterrupt(Process lInventoryProcess)
    {
        try
        {
            lInventoryProcess.Kill(true);
        }
        catch (Exception lInventoryException)
            when (lInventoryException is System.ComponentModel.Win32Exception
                or InvalidOperationException
                or NotSupportedException)
        {
        }
    }
}
