using System.Diagnostics;

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
            if (lEmployerHeld.Contains(lEmployerWorkId) && LEmployerProcess.LEmployerProcessCheck(lEmployerProcess))
            {
                LEmployerProcess.LEmployerProcessSuspend(lEmployerProcess);
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
                && LEmployerProcess.LEmployerProcessCheck(lEmployerProcess)
                && !LEmployerProcess.LEmployerProcessSuspend(lEmployerProcess))
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
                && LEmployerProcess.LEmployerProcessCheck(lEmployerProcess)
                && !LEmployerProcess.LEmployerProcessResume(lEmployerProcess))
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
                && LEmployerProcess.LEmployerProcessCheck(lEmployerProcess);
        }
    }
}
