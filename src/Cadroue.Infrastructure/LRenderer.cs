using System;
using System.Threading.Tasks;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRenderer
{
    public const string LRendererFlyleafToken = "Flyleaf";
    public const string LRendererMpvToken = "Mpv";

    public static Func<string> LRendererEngineSeam { get; set; } = () => LRendererFlyleafToken;

    private static LPreviewEngine lRendererEnginePreview = LPreviewEngine.LPreviewEngineFlyleaf;
    private static bool lRendererMpvAvailable;

    private static readonly object lRendererCheckGate = new();
    private static Task<LMpvProbe>? lRendererCheckTask;

    public static event Action? LRendererEngineChanged;

    public static Action? LRendererFlyleafSeam { get; set; }

    public static Action<Action>? LRendererDispatchSeam { get; set; }

    public static LPreviewEngine LRendererEngineRead() => lRendererEnginePreview;

    public static void LRendererEngineStart()
    {
        LRendererFlyleafStart();
        LRendererEngineSet(LRendererPreferenceRead());
        if (LMpvLibrary.LMpvLibraryCheck() && LMpvResult.LMpvResultRead() == LMpvProbe.LMpvProbeUnknown)
        {
            _ = LRendererEngineCheck();
        }
    }

    private static void LRendererFlyleafStart()
    {
        try
        {
            LRendererFlyleafSeam?.Invoke();
            LTrace.LTraceVerboseCallback = LRendererLog.LRendererVerboseApply;
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Renderer startup failed", lException);
        }
    }

    public static void LRendererEngineSet(LPreviewEngine lRendererEngine)
    {
        bool lRendererAvailable = LMpvAvailability.LMpvAvailableCheck();
        bool lRendererAvailableChanged = lRendererAvailable != lRendererMpvAvailable;
        lRendererMpvAvailable = lRendererAvailable;
        LPreviewEngine lRendererResolved =
            lRendererEngine == LPreviewEngine.LPreviewEngineMpv && lRendererAvailable
                ? LPreviewEngine.LPreviewEngineMpv
                : LPreviewEngine.LPreviewEngineFlyleaf;
        if (lRendererResolved == lRendererEnginePreview && !lRendererAvailableChanged)
        {
            return;
        }

        lRendererEnginePreview = lRendererResolved;
        LRendererEngineChanged?.Invoke();
    }

    public static void LRendererEngineUpdate() => LRendererEngineSet(LRendererPreferenceRead());

    public static async Task<bool> LRendererMpvCheck()
    {
        if (!LMpvLibrary.LMpvLibraryCheck())
        {
            return false;
        }

        LMpvProbe lRendererProbe = LMpvResult.LMpvResultRead();
        if (lRendererProbe == LMpvProbe.LMpvProbeUnknown)
        {
            lRendererProbe = await LRendererEngineCheck();
        }

        return lRendererProbe == LMpvProbe.LMpvProbeUsable;
    }

    private static LPreviewEngine LRendererPreferenceRead() =>
        string.Equals(
            LRendererEngineSeam(),
            LRendererMpvToken,
            StringComparison.Ordinal)
            ? LPreviewEngine.LPreviewEngineMpv
            : LPreviewEngine.LPreviewEngineFlyleaf;

    public static Task<LMpvProbe> LRendererEngineCheck()
    {
        lock (lRendererCheckGate)
        {
            if (lRendererCheckTask is { IsCompleted: false })
            {
                return lRendererCheckTask;
            }

            lRendererCheckTask = Task.Run(() =>
            {
                LMpvProbe lRendererProbed;
                try
                {
                    lRendererProbed = LMpvAvailability.LMpvCheck();
                }
                catch
                {
                    lRendererProbed = LMpvProbe.LMpvProbeUnusable;
                }

                LMpvResult.LMpvResultSave(lRendererProbed);
                if (LRendererDispatchSeam is { } lRendererDispatch)
                {
                    lRendererDispatch(LRendererEngineUpdate);
                }
                else
                {
                    LRendererEngineUpdate();
                }

                return lRendererProbed;
            });

            return lRendererCheckTask;
        }
    }
}
