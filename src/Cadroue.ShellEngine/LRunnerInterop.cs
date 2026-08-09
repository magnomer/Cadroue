using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed partial class LRunner
{
    private void LRunnerProcessResume()
    {
        bool lRunnerAllResumed = true;
        foreach (KeyValuePair<Guid, Process> lRunnerEntry in lRunnerProcesses)
        {
            Process pProcess = lRunnerEntry.Value;
            if (pProcess.HasExited)
            {
                continue;
            }

            if (!LRunnerProcessResume(pProcess))
            {
                lRunnerAllResumed = false;
                continue;
            }

            lRunnerItems.TryGetValue(lRunnerEntry.Key, out LWorkItem? pWorkItem);
            LRunnerMessageSet(pWorkItem, string.Empty);
        }

        if (lRunnerAllResumed)
        {
            lRunnerSuspended = false;
        }
        else
        {
            LRunnerRecord("Resume failed: a live process stayed suspended; the paused flag is kept so the next Play retries the resume");
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
