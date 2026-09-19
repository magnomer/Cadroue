using System;
using System.Threading.Tasks;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public sealed record LRendererFacts(
    string LRendererReason,
    string LRendererProcessorSetting,
    string LRendererAcceleration,
    bool LRendererFilterSynced)
{
    public string? LRendererProcessorActive { get; init; }
    public bool? LRendererAccelerated { get; init; }
    public string? LRendererAdapter { get; init; }
    public string? LRendererPixel { get; init; }
    public string? LRendererColorRange { get; init; }
    public string? LRendererColorSpace { get; init; }
    public double? LRendererContrast { get; init; }
    public string? LRendererConfig { get; init; }
}

public static class LRenderer
{
    public static void LRendererFactsRecord(LRendererFacts lFacts, double? lMilliseconds = null)
    {
        if (!LTrace.LTraceCheck(LTraceKind.LTraceUi))
        {
            return;
        }

        string lVerdict = lFacts.LRendererProcessorActive is null
            ? "renderer not ready"
            : lFacts.LRendererContrast is null
                ? "no contrast filter reached the engine"
                : $"contrast {lFacts.LRendererContrast} reached the engine";
        string lDecode = lFacts.LRendererAccelerated switch
        {
            null => "decoder not ready",
            true => "HARDWARE (D3D11VA)",
            false => "SOFTWARE, hardware decode did not engage"
        };
        LTrace.LTraceRecord(
            LTraceKind.LTraceUi,
            lFacts.LRendererReason,
            $"verdict {lVerdict}\n"
            + $"acceleration requested {lFacts.LRendererAcceleration}, "
            + $"processor requested {lFacts.LRendererProcessorSetting}, "
            + $"processor in use {lFacts.LRendererProcessorActive ?? "none"}\n"
            + $"decode path {lDecode}, adapter {lFacts.LRendererAdapter ?? "none"}\n"
            + $"pixel format {lFacts.LRendererPixel ?? "unknown"}, "
            + $"color range {lFacts.LRendererColorRange ?? "unknown"}, "
            + $"color space {lFacts.LRendererColorSpace ?? "unknown"}\n"
            + $"sync vp filters {lFacts.LRendererFilterSynced}"
            + (lFacts.LRendererConfig is null ? string.Empty : $"\n{lFacts.LRendererConfig}"),
            lMilliseconds);
    }

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

    public static Action<bool>? LRendererVerboseSeam { get; set; }

    public static void LRendererToolAttach() => LTool.LToolFolderSource = () => LRendererFolderCurrent;

    public static string LRendererFfmpegResolve(string lRendererDefaultPath)
    {
        if (LRendererLibrary.LRendererFolderValidate(LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder))
        {
            return LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder;
        }

        if (LRendererLibrary.LRendererFolderValidate(LRendererSettingsCurrent.LRendererLibraryFolder))
        {
            return LRendererSettingsCurrent.LRendererLibraryFolder!;
        }

        return LRendererLibrary.LRendererFolderFind() ?? lRendererDefaultPath;
    }

    public static string LRendererLogResolve(bool lRendererVerbose) =>
        lRendererVerbose
            ? Path.Combine(LRendererLogCreate(), $"flyleaf-debug-{Environment.ProcessId}.log")
            : string.Empty;

    private static void LRendererVerboseApply(bool lRendererVerbose)
    {
        try
        {
            LRendererVerboseSeam?.Invoke(lRendererVerbose);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Renderer log switch failed", lException);
        }
    }

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
        try
        {
            LRendererFlyleafSeam?.Invoke();
            LTrace.LTraceVerboseCallback = LRendererVerboseApply;
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Renderer startup failed", lException);
        }

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
