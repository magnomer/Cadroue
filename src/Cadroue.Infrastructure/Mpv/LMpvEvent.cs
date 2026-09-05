using System.Runtime.InteropServices;
using System.Threading;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LMpv
{
    private const string LMpvProbeSource = "av://lavfi:testsrc=d=1:s=64x64";
    private static readonly TimeSpan LMpvProbeBudget = TimeSpan.FromSeconds(4);

    private const int LMpvEventNone = 0;
    private const int LMpvEventShutdown = 1;
    private const int LMpvEventStarted = 6;
    private const int LMpvEventEnd = 7;
    private const int LMpvEventLoaded = 8;

    public LMpvProbe LMpvMediaCheck(string lPath, TimeSpan lBudget, CancellationToken lToken)
    {
        LMpvEventClear();
        LMpvOpen(lPath);
        return LMpvLoadedScan(lBudget, lToken);
    }

    public static LMpvProbe LMpvCheck()
    {
        try
        {
            using LMpv lMpv = new();
            lMpv.LMpvContextCreate(nint.Zero);
            lMpv.LMpvOpen(LMpvProbeSource);
            return lMpv.LMpvLoadedScan(LMpvProbeBudget, CancellationToken.None);
        }
        catch (DllNotFoundException)
        {
            return LMpvProbe.LMpvProbeUnusable;
        }
        catch (InvalidOperationException)
        {
            return LMpvProbe.LMpvProbeUnusable;
        }
        catch
        {
            return LMpvProbe.LMpvProbeUnknown;
        }
    }

    public void LMpvEventClear()
    {
        LMpvContextValidate();
        while (true)
        {
            nint lEvent = LMpvNative.mpv_wait_event(lMpvContext, 0);
            if (lEvent == nint.Zero)
            {
                return;
            }

            if (Marshal.ReadInt32(lEvent) == LMpvEventNone)
            {
                return;
            }
        }
    }

    public LMpvProbe LMpvLoadedScan(TimeSpan lBudget, CancellationToken lToken)
    {
        LMpvContextValidate();
        DateTime lDeadline = DateTime.UtcNow + lBudget;
        bool lStarted = false;
        while (true)
        {
            if (lToken.IsCancellationRequested)
            {
                return LMpvProbe.LMpvProbeUnusable;
            }

            double lRemaining = (lDeadline - DateTime.UtcNow).TotalSeconds;
            if (lRemaining <= 0)
            {
                return LMpvProbe.LMpvProbeUnusable;
            }

            nint lEvent = LMpvNative.mpv_wait_event(lMpvContext, Math.Min(0.1, lRemaining));
            if (lEvent == nint.Zero)
            {
                continue;
            }

            int lEventId = Marshal.ReadInt32(lEvent);
            if (lEventId == LMpvEventStarted)
            {
                lStarted = true;
                continue;
            }

            if (lEventId == LMpvEventLoaded)
            {
                return LMpvProbe.LMpvProbeUsable;
            }

            if (lEventId == LMpvEventShutdown)
            {
                return LMpvProbe.LMpvProbeUnusable;
            }

            if (lEventId == LMpvEventEnd && lStarted)
            {
                return LMpvProbe.LMpvProbeUnusable;
            }
        }
    }
}
