using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cadroue.Media;

public static class LEmployerChild
{
    private static readonly object lEmployerGate = new();
    private static readonly Dictionary<Guid, Process> lEmployerChildren = new();
    private static readonly HashSet<Guid> lEmployerHeld = new();

    internal static void LEmployerChildAdd(Guid lEmployerWorkId, Process lEmployerProcess)
    {
        lock (lEmployerGate)
        {
            lEmployerChildren[lEmployerWorkId] = lEmployerProcess;
            if (lEmployerHeld.Contains(lEmployerWorkId) && LEmployerProcessCheck(lEmployerProcess))
            {
                LEmployerProcessSuspend(lEmployerProcess);
            }
        }
    }

    internal static void LEmployerChildRemove(Guid lEmployerWorkId)
    {
        lock (lEmployerGate)
        {
            lEmployerChildren.Remove(lEmployerWorkId);
        }
    }

    public static bool LEmployerChildSuspend(Guid lEmployerWorkId)
    {
        lock (lEmployerGate)
        {
            if (lEmployerHeld.Contains(lEmployerWorkId))
            {
                return true;
            }

            if (lEmployerChildren.TryGetValue(lEmployerWorkId, out Process? lEmployerProcess)
                && LEmployerProcessCheck(lEmployerProcess)
                && !LEmployerProcessSuspend(lEmployerProcess))
            {
                return false;
            }

            lEmployerHeld.Add(lEmployerWorkId);
            return true;
        }
    }

    public static bool LEmployerChildResume(Guid lEmployerWorkId)
    {
        lock (lEmployerGate)
        {
            if (!lEmployerHeld.Contains(lEmployerWorkId))
            {
                return true;
            }

            if (lEmployerChildren.TryGetValue(lEmployerWorkId, out Process? lEmployerProcess)
                && LEmployerProcessCheck(lEmployerProcess)
                && !LEmployerProcessResume(lEmployerProcess))
            {
                return false;
            }

            lEmployerHeld.Remove(lEmployerWorkId);
            return true;
        }
    }

    public static bool LEmployerChildCheck(Guid lEmployerWorkId)
    {
        lock (lEmployerGate)
        {
            return lEmployerChildren.TryGetValue(lEmployerWorkId, out Process? lEmployerProcess)
                && LEmployerProcessCheck(lEmployerProcess);
        }
    }

    private static bool LEmployerProcessCheck(Process lEmployerProcess)
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

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtSuspendProcess(IntPtr lRunnerProcessHandle);

    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern int NtResumeProcess(IntPtr lRunnerProcessHandle);

    private static bool LEmployerProcessSuspend(Process lEmployerProcess)
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

    private static bool LEmployerProcessResume(Process lEmployerProcess)
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
