using System;
using System.Threading.Tasks;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRenderer
{
    public static LRendererSettings LRendererSettingsCurrent { get; private set; } = LRendererSettings.LRendererDefaultCreate();

    public static string LRendererFolderCurrent =>
        string.IsNullOrWhiteSpace(LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder)
            ? LRendererSettingsCurrent.LRendererLibraryFolder ?? string.Empty
            : LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder;

    public static string LRendererProgramCurrent =>
        LRendererLibrary.LRendererProgramRead(LRendererFolderCurrent);

    private static LPreviewEngine lRendererEnginePreview = LPreviewEngine.LPreviewEngineFlyleaf;

    private static readonly object lRendererCheckGate = new();
    private static Task<LMpvProbe>? lRendererCheckRun;

    public static event Action? LRendererEngineChange;

    public static Action? LRendererFlyleafSeam { get; set; }

    public static void LRendererSettingsLoad() =>
        LRendererSettingsCurrent = LRendererSettingsStore.LRendererSettingsLoad();

    public const string LRendererEngineFlyleafToken = "Flyleaf";
    public const string LRendererEngineMpvToken = "Mpv";

    public static LPreviewEngine LRendererEngineRead() => lRendererEnginePreview;

    public static void LRendererEngineStart()
    {
        LRendererFlyleafSeam?.Invoke();
        LRendererEngineSet(LRendererPreferenceRead());
    }

    public static void LRendererEngineSet(LPreviewEngine lRendererEngine)
    {
        LPreviewEngine lRendererResolved =
            lRendererEngine == LPreviewEngine.LPreviewEngineMpv && LMpv.LMpvInstalledCheck()
                ? LPreviewEngine.LPreviewEngineMpv
                : LPreviewEngine.LPreviewEngineFlyleaf;
        if (lRendererResolved == lRendererEnginePreview)
        {
            return;
        }

        lRendererEnginePreview = lRendererResolved;
        LRendererEngineChange?.Invoke();
    }

    private static LPreviewEngine LRendererPreferenceRead() =>
        string.Equals(
            LPreference.LPreferenceStateCurrent.LPreferencePreviewEngine,
            LRendererEngineMpvToken,
            StringComparison.Ordinal)
            ? LPreviewEngine.LPreviewEngineMpv
            : LPreviewEngine.LPreviewEngineFlyleaf;

    public static Task<LMpvProbe> LRendererEngineCheck()
    {
        lock (lRendererCheckGate)
        {
            if (lRendererCheckRun is { IsCompleted: false })
            {
                return lRendererCheckRun;
            }

            lRendererCheckRun = Task.Run(() =>
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
                return lRendererProbed;
            });

            return lRendererCheckRun;
        }
    }
}
