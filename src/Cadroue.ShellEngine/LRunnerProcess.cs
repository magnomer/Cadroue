using System.Collections.Concurrent;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed class LRunnerProcess
{
    private readonly object lRunnerProcessGate;
    private readonly LScheduleContract lRunnerProcessSchedule;
    private readonly Action<Action> lRunnerProcessPost;
    private readonly ConcurrentDictionary<Guid, LWorkItem> lRunnerProcessItems = new();
    private bool lRunnerProcessSuspended;

    internal LRunnerProcess(object lRunnerGate, LScheduleContract lSchedule, Action<Action> lRunnerPostAction)
    {
        lRunnerProcessGate = lRunnerGate;
        lRunnerProcessSchedule = lSchedule;
        lRunnerProcessPost = lRunnerPostAction;
    }

    public bool LRunnerProcessSuspended => lRunnerProcessSuspended;

    internal void LRunnerProcessSuspend()
    {
        lock (lRunnerProcessGate)
        {
            if (lRunnerProcessSuspended)
            {
                return;
            }

            bool lRunnerLiveExisted = false;
            bool lRunnerSuspendedAny = false;
            var lRunnerHeld = new List<Guid>();
            foreach (KeyValuePair<Guid, LWorkItem> lRunnerEntry in lRunnerProcessItems)
            {
                Guid lWorkId = lRunnerEntry.Key;
                bool lRunnerLive = LEmployerEngine.LEmployerLiveSeam(lWorkId);
                lRunnerLiveExisted |= lRunnerLive;
                if (!LEmployerEngine.LEmployerSuspendSeam(lWorkId))
                {
                    continue;
                }

                lRunnerSuspendedAny |= lRunnerLive;
                lRunnerHeld.Add(lWorkId);
            }

            if (lRunnerSuspendedAny || !lRunnerLiveExisted)
            {
                lRunnerProcessSuspended = true;
                foreach (Guid lWorkId in lRunnerHeld)
                {
                    LRunnerMessageSet(lRunnerProcessItems[lWorkId], "Suspended");
                }
            }
            else
            {
                foreach (Guid lWorkId in lRunnerHeld)
                {
                    LEmployerEngine.LEmployerResumeSeam(lWorkId);
                }

                LTraceEngine.LTraceInfoSeam(
                    "Pause failed: no running process could be suspended; the queue keeps encoding");
            }
        }
    }

    internal void LRunnerProcessAttach(LWorkItem lWorkItem)
    {
        lock (lRunnerProcessGate)
        {
            lRunnerProcessItems[lWorkItem.LWorkId] = lWorkItem;
            if (lRunnerProcessSuspended && LEmployerEngine.LEmployerSuspendSeam(lWorkItem.LWorkId))
            {
                LRunnerMessageSet(lWorkItem, "Suspended");
            }
        }
    }

    internal void LRunnerProcessDetach(Guid lWorkId)
    {
        lock (lRunnerProcessGate)
        {
            if (lRunnerProcessItems.TryRemove(lWorkId, out _))
            {
                LEmployerEngine.LEmployerResumeSeam(lWorkId);
            }
        }
    }

    internal void LRunnerProcessResume()
    {
        lock (lRunnerProcessGate)
        {
            bool lRunnerAllResumed = true;
            foreach (KeyValuePair<Guid, LWorkItem> lRunnerEntry in lRunnerProcessItems)
            {
                Guid lWorkId = lRunnerEntry.Key;
                if (!LEmployerEngine.LEmployerResumeSeam(lWorkId))
                {
                    lRunnerAllResumed = false;
                    continue;
                }

                LRunnerMessageSet(lRunnerEntry.Value, string.Empty);
            }

            if (lRunnerAllResumed)
            {
                lRunnerProcessSuspended = false;
            }
            else
            {
                LTraceEngine.LTraceInfoSeam(
                    "Resume failed: a live process stayed suspended; " +
                    "the paused flag is kept so the next Play retries the resume");
            }
        }
    }

    internal void LRunnerProcessReset()
    {
        lock (lRunnerProcessGate)
        {
            lRunnerProcessSuspended = false;
        }
    }

    private void LRunnerMessageSet(LWorkItem pWorkItem, string pMessage)
    {
        lRunnerProcessPost(() =>
        {
            pWorkItem.LWorkRunState.LWorkMessage = pMessage;
            lRunnerProcessSchedule.LScheduleItemRaise(pWorkItem, LScheduleNotice.LScheduleNoticeStatus);
        });
    }
}
