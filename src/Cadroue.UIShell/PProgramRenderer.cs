using System;
using System.IO;

using Cadroue.Application;
using Cadroue.Infrastructure;

using FlyleafLib;

namespace Cadroue.UIShell;

public partial class PProgram
{
    private static void LRendererFlyleafStart()
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
            else if (LRendererLibrary.LRendererFolderValidate(Cadroue.Infrastructure.LRenderer.LRendererSettingsCurrent.LRendererLibraryFolder))
            {
                lRendererEngineConfig.FFmpegPath = Cadroue.Infrastructure.LRenderer.LRendererSettingsCurrent.LRendererLibraryFolder;
            }
            else
            {
                string? lRendererLocalPath = LRendererLibrary.LRendererFolderFind();
                if (lRendererLocalPath is not null)
                {
                    lRendererEngineConfig.FFmpegPath = lRendererLocalPath;
                }
            }

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
        lRendererConfig.LogOutput = Path.Combine(lRendererLogFolder, $"flyleaf-debug-{Environment.ProcessId}.log");
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
