using System;
using System.Threading.Tasks;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRenderer
{
    public static LRendererSettings LRendererSettingsCurrent { get; private set; } =
        LRendererSettings.LRendererDefaultCreate();

    public static string LRendererFolderCurrent =>
        string.IsNullOrWhiteSpace(LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder)
            ? LRendererSettingsCurrent.LRendererLibraryFolder ?? string.Empty
            : LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder;

    public static string LRendererProgramCurrent =>
        LRendererLibrary.LRendererProgramRead(LRendererFolderCurrent);

    private static LPreviewEngine lRendererEnginePreview = LPreviewEngine.LPreviewEngineFlyleaf;
    private static bool lRendererMpvAvailable;

    private static readonly object lRendererCheckGate = new();
    private static Task<LMpvProbe>? lRendererCheckTask;

    public static event Action? LRendererEngineChange;

    public static Action? LRendererFlyleafSeam { get; set; }

    public static Action<Action>? LRendererDispatchSeam { get; set; }

    public static void LRendererSettingsLoad() =>
        LRendererSettingsCurrent = LRendererSettingsStore.LRendererSettingsLoad();

    public const string LRendererFlyleafToken = "Flyleaf";
    public const string LRendererMpvToken = "Mpv";

    public static LPreviewEngine LRendererEngineRead() => lRendererEnginePreview;

    public static string LRendererLogCreate()
    {
        string lRendererLogFolder = LFlyleaf.LFlyleafRootRead();
        Directory.CreateDirectory(lRendererLogFolder);
        return lRendererLogFolder;
    }

    public static void LRendererEngineStart()
    {
        LRendererFlyleafSeam?.Invoke();
        LRendererEngineSet(LRendererPreferenceRead());
        if (LMpv.LMpvLibraryCheck() && LMpv.LMpvResultRead() == LMpvProbe.LMpvProbeUnknown)
        {
            _ = LRendererEngineCheck();
        }
    }

    public static void LRendererEngineSet(LPreviewEngine lRendererEngine)
    {
        bool lRendererAvailable = LMpv.LMpvAvailableCheck();
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
        LRendererEngineChange?.Invoke();
    }

    public static void LRendererEngineUpdate() => LRendererEngineSet(LRendererPreferenceRead());

    public static async Task<bool> LRendererMpvCheck()
    {
        if (!LMpv.LMpvLibraryCheck())
        {
            return false;
        }

        LMpvProbe lRendererProbe = LMpv.LMpvResultRead();
        if (lRendererProbe == LMpvProbe.LMpvProbeUnknown)
        {
            lRendererProbe = await LRendererEngineCheck();
        }

        return lRendererProbe == LMpvProbe.LMpvProbeUsable;
    }

    private static LPreviewEngine LRendererPreferenceRead() =>
        string.Equals(
            LPreference.LPreferenceStateCurrent.LPreferencePreviewEngine,
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
                    lRendererProbed = LMpv.LMpvCheck();
                }
                catch
                {
                    lRendererProbed = LMpvProbe.LMpvProbeUnusable;
                }

                LMpv.LMpvResultSave(lRendererProbed);
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
