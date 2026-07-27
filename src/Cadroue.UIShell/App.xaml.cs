using System;
using System.Threading.Tasks;
using System.Windows;
using FlyleafLib;

namespace Cadroue.UIShell;

public partial class App : Application
{
    public static LRendererSettings LRendererSettingsCurrent { get; private set; } = LRendererSettings.LRendererDefaultCreate();
    public static LPreferenceState LPreferenceStateCurrent { get; private set; } = LPreferenceState.LPreferenceDefaultCreate();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, pEvent) =>
        {
            LAppLog.LError("Unhandled UI exception", pEvent.Exception);
            pEvent.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, pEvent) =>
            LAppLog.LError("Unhandled application exception", pEvent.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, pEvent) =>
        {
            LAppLog.LError("Unobserved task exception", pEvent.Exception);
            pEvent.SetObserved();
        };

        LAppLog.LInfo("Application started");
        LRendererSettingsCurrent = LRendererSettingsStore.LRendererSettingsLoad();
        LPreferenceStateCurrent = LPreferenceStateStore.LPreferenceStateLoad();
        LRendererFlyleafStart();
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

    private static void LRendererFlyleafStart()
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
                string? lRendererLocalPath = LRendererSettings.LRendererFolderFind();
                if (lRendererLocalPath is not null)
                {
                    lRendererEngineConfig.FFmpegPath = lRendererLocalPath;
                }
            }

            Engine.Start(lRendererEngineConfig);
        }
        catch (Exception lException)
        {
            LAppLog.LError("Renderer startup failed", lException);
        }
    }
}
