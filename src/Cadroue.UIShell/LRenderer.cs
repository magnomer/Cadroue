using System;
using System.IO;

using Cadroue.Core;
using Cadroue.Infrastructure;

using FlyleafLib;

namespace Cadroue.UIShell;

internal static class LRenderer
{
    internal static LRendererSettings LRendererSettingsCurrent { get; private set; } = LRendererSettings.LRendererDefaultCreate();

    internal static string LRendererFolderCurrent =>
        string.IsNullOrWhiteSpace(LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder)
            ? LRendererSettingsCurrent.LRendererLibraryFolder ?? string.Empty
            : LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder;

    internal static string LRendererProgramCurrent =>
        LRendererLibrary.LRendererProgramRead(LRendererFolderCurrent);

    internal static void LRendererSettingsLoad() =>
        LRendererSettingsCurrent = LRendererSettingsStore.LRendererSettingsLoad();

    internal static void LRendererFlyleafStart()
    {
        try
        {
            var lRendererEngineConfig = new EngineConfig
            {
                UIRefresh = false,
                UIRefreshInterval = 250
            };
            LRendererLogApply(lRendererEngineConfig, LTrace.LTraceVerbose);

            if (LRendererLibrary.LRendererFolderValidate(LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder))
            {
                lRendererEngineConfig.FFmpegPath = LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder;
            }
            else if (LRendererLibrary.LRendererFolderValidate(LRendererSettingsCurrent.LRendererLibraryFolder))
            {
                lRendererEngineConfig.FFmpegPath = LRendererSettingsCurrent.LRendererLibraryFolder;
            }
            else
            {
                string? lRendererLocalPath = LRendererLibrary.LRendererFolderFind();
                if (lRendererLocalPath is not null)
                {
                    lRendererEngineConfig.FFmpegPath = lRendererLocalPath;
                }
            }

            LTraceLog.LTraceInfoRecord(LFlyleaf.LFlyleafReportRead(typeof(Engine).Assembly));
            Engine.Start(lRendererEngineConfig);
            LTrace.LTraceVerboseCallback = LRendererVerboseApply;
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Renderer startup failed", lException);
        }
    }

    private static void LRendererLogApply(EngineConfig lRendererConfig, bool lRendererVerbose)
    {
        if (!lRendererVerbose)
        {
            lRendererConfig.LogLevel = LogLevel.Quiet;
            lRendererConfig.LogOutput = string.Empty;
            return;
        }

        string lRendererLogFolder = LFlyleaf.LFlyleafRootRead();
        Directory.CreateDirectory(lRendererLogFolder);
        lRendererConfig.LogLevel = LogLevel.Debug;
        lRendererConfig.LogOutput = Path.Combine(lRendererLogFolder, "flyleaf-debug.log");
    }

    private static void LRendererVerboseApply(bool lRendererVerbose)
    {
        try
        {
            LRendererLogApply(Engine.Config, lRendererVerbose);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Renderer log switch failed", lException);
        }
    }
}
