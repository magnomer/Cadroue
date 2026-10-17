using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cadroue.Media;

internal static class LEmployerProcess
{
    internal static readonly TimeSpan LEmployerExitWait = TimeSpan.FromSeconds(5);

    internal static bool LEmployerProcessCheck(Process lEmployerProcess)
    {
        try
        {
            return !lEmployerProcess.HasExited;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    internal static async Task<bool> LEmployerProcessInterrupt(Process lEmployerProcess)
    {
        try
        {
            if (!lEmployerProcess.HasExited)
            {
                lEmployerProcess.Kill(true);
            }
        }
        catch (Exception lEmployerException)
            when (lEmployerException is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
        }

        try
        {
            using var lEmployerWait = new CancellationTokenSource(LEmployerExitWait);
            await lEmployerProcess.WaitForExitAsync(lEmployerWait.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception lEmployerException)
            when (lEmployerException is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return true;
        }
    }

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtSuspendProcess(IntPtr lRunnerProcessHandle);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtResumeProcess(IntPtr lRunnerProcessHandle);

    internal static bool LEmployerProcessSuspend(Process lEmployerProcess)
    {
        try
        {
            return NtSuspendProcess(lEmployerProcess.Handle) == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    internal static bool LEmployerProcessResume(Process lEmployerProcess)
    {
        try
        {
            return NtResumeProcess(lEmployerProcess.Handle) == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
