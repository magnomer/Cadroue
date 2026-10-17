using System.Threading;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LMpvAvailability
{
    private const string LMpvProbeSource = "av://lavfi:testsrc=d=1:s=64x64";
    private static readonly TimeSpan LMpvProbeBudget = TimeSpan.FromSeconds(4);

    public static bool LMpvAvailableCheck() =>
        LMpvLibrary.LMpvLibraryCheck() && LMpvResult.LMpvResultRead() == LMpvProbe.LMpvProbeUsable;

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
}
