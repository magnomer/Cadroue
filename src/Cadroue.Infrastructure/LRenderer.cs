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

    public static event Action? LRendererEngineChange;

    public static void LRendererSettingsLoad() =>
        LRendererSettingsCurrent = LRendererSettingsStore.LRendererSettingsLoad();

    public static LPreviewEngine LRendererEnginePreviewRead() => lRendererEnginePreview;

    public static void LRendererEngineStart()
    {
        LMpvProbe lRendererOutcome = LMpv.LMpvResultRead();
        if (lRendererOutcome != LMpvProbe.LMpvProbeUnknown)
        {
            LRendererEngineApply(lRendererOutcome);
            return;
        }

        _ = Task.Run(() =>
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
            LRendererEngineApply(lRendererProbed);
            LRendererEngineChange?.Invoke();
        });
    }

    private static void LRendererEngineApply(LMpvProbe lRendererOutcome) =>
        lRendererEnginePreview = lRendererOutcome == LMpvProbe.LMpvProbeUsable
            ? LPreviewEngine.LPreviewEngineMpv
            : LPreviewEngine.LPreviewEngineFlyleaf;
}
