using System;
using System.Windows;
using FlyleafLib;

namespace Cadroue.UIShell;

public partial class App : Application
{
    public static LRendererSettings LRendererSettingsCurrent { get; private set; } = LRendererSettings.LRendererSettingsDefaultCreate();
    public static LPreferenceState LPreferenceStateCurrent { get; private set; } = LPreferenceState.LPreferenceStateDefaultCreate();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LRendererSettingsCurrent = LRendererSettingsStore.LRendererSettingsLoad();
        LPreferenceStateCurrent = LPreferenceStateStore.LPreferenceStateLoad();
        LRendererFlyleafStartAttempt();
    }

    public static void LPreferenceStateSet(LPreferenceState lPreferenceState)
    {
        lPreferenceState.LPreferenceNormalize();
        LPreferenceStateCurrent = lPreferenceState;
        LPreferenceStateStore.LPreferenceStateSave(LPreferenceStateCurrent);
    }

    public static void LPreferenceVolumeSet(double lPreferenceVolume)
    {
        LPreferenceStateSet(LPreferenceStateCurrent.LPreferenceVolumeChange(lPreferenceVolume));
    }

    private static void LRendererFlyleafStartAttempt()
    {
        try
        {
            var lRendererEngineConfig = new EngineConfig
            {
                UIRefresh = false,
                UIRefreshInterval = 250
            };

            if (LRendererSettingsCurrent.LRendererFfmpegLibraryFolderCustomReady)
            {
                lRendererEngineConfig.FFmpegPath = LRendererSettingsCurrent.LRendererFfmpegLibraryFolder;
            }
            else
            {
                string? lRendererLocalPath = LRendererSettings.LRendererFfmpegLibraryFolderLocalFind();
                if (lRendererLocalPath is not null)
                {
                    lRendererEngineConfig.FFmpegPath = lRendererLocalPath;
                }
            }

            Engine.Start(lRendererEngineConfig);
        }
        catch
        {
        }
    }
}
