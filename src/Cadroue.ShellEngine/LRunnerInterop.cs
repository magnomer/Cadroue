using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cadroue.ShellEngine;

public sealed partial class LRunner
{
    private void LRunnerProcessResume()
    {
        Process? pProcess = lRunnerProcess;
        if (pProcess is null || pProcess.HasExited)
        {
            lRunnerSuspended = false;
            return;
        }

        if (LRunnerProcessResume(pProcess))
        {
            lRunnerSuspended = false;
            LRunnerMessageSet(lRunnerItem, string.Empty);
        }
    }

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtSuspendProcess(IntPtr lRunnerProcessHandle);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtResumeProcess(IntPtr lRunnerProcessHandle);

    private static bool LRunnerProcessSuspend(Process pProcess)
    {
        try
        {
            return NtSuspendProcess(pProcess.Handle) == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool LRunnerProcessResume(Process pProcess)
    {
        try
        {
            return NtResumeProcess(pProcess.Handle) == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
