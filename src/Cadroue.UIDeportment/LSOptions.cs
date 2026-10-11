using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum LSOptionsPage
{
    LSOptionsPageGeneral,
    LSOptionsPageSystem,
    LSOptionsPagePlayback,
    LSOptionsPageTimeline,
    LSOptionsPageWork
}

public sealed record LSOptionsLevel(
    string LSOptionsLevelText,
    int LSOptionsLevelIndex,
    Action<int> LSOptionsLevelChoice);

public sealed class LSOptions
{
    private static readonly IReadOnlyDictionary<bool, string> lsOptionsRecordKeys = new Dictionary<bool, string>
    {
        [true] = "Options.System.FileRecordWorkspace",
        [false] = "Options.System.FileRecordBeside",
    };

    private readonly CUsher lsOptionsUsher;
    private readonly LPreferenceState lsOptionsDraft;
    private LSOptionsPage lsOptionsPage;
    private bool lsOptionsMpvEnabled;

    public event Action<bool>? LSOptionsMpvChanged;

    public LSOptions(LStrip lStrip)
        : this(lStrip, LPreference.LPreferenceStateCurrent.LPreferenceClone(), LMpv.LMpvAvailableCheck())
    {
    }

    public LSOptions(LStrip lStrip, LPreferenceState lDraft, bool lMpvEnabled)
    {
        lsOptionsUsher = lStrip.LStripUsher;
        lsOptionsDraft = lDraft;
        lsOptionsMpvEnabled = lMpvEnabled;
    }

    public LPreferenceState LSOptionsDraft => lsOptionsDraft;

    public LSOptionsPage LSOptionsPage => lsOptionsPage;

    public bool LSOptionsMpvEnabled => lsOptionsMpvEnabled;

    public void LSOptionsPageSelect(LSOptionsPage lPage) => lsOptionsPage = lPage;

    public bool LSOptionsFolderExist(string lFolder) => lsOptionsUsher.CUsherFolderExist(lFolder);

    public string? LSOptionsFolderOpen(string lFolder, string lFallback, string lLabel) =>
        lsOptionsUsher.CUsherFolderOpen(string.IsNullOrWhiteSpace(lFolder) ? lFallback : lFolder, lLabel);

    public string LSOptionsEngineRead() =>
        lsOptionsMpvEnabled && string.Equals(lsOptionsDraft.LPreferencePreviewEngine, "Mpv", StringComparison.Ordinal)
            ? "Mpv"
            : "Flyleaf";

    public void LSOptionsMpvSet(bool lEnabled)
    {
        lsOptionsMpvEnabled = lEnabled;
        LSOptionsMpvChanged?.Invoke(lEnabled);
    }

    public async Task LSOptionsMpvUpdate()
    {
        bool lUsable;
        try
        {
            lUsable = await LRenderer.LRendererMpvCheck();
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Options mpv check failed", lException);
            lUsable = false;
        }

        LSOptionsMpvSet(lUsable);
    }

    public static string LSOptionsMpvResolve() =>
        LMpv.LMpvInstalledCheck() ? "Options.System.ReinstallMpv" : "Options.System.DownloadMpv";

    public static string LSOptionsFlyleafResolve() =>
        LFlyleaf.LFlyleafInstalledCheck() ? "Options.System.ReinstallFlyleaf" : "Options.System.InstallFlyleaf";

    public static string LSOptionsFfmpegResolve(string lFolder)
    {
        if (string.IsNullOrWhiteSpace(lFolder))
        {
            return "Options.System.FFmpegBlank";
        }

        bool lProgramReady = LRendererLibrary.LRendererProgramExist(lFolder);
        bool lLibraryReady = LRendererLibrary.LRendererFolderValidate(lFolder);
        if (lProgramReady && lLibraryReady)
        {
            return "Options.System.FFmpegReady";
        }

        if (lProgramReady)
        {
            return "Options.System.FFmpegProgramOnly";
        }

        return lLibraryReady ? "Options.System.FFmpegLibraryOnly" : "Options.System.FFmpegMissing";
    }

    public static bool LSOptionsAppliedCheck(string lFolder) =>
        string.Equals(LDepot.LDepotRootResolve(lFolder), LDepot.LDepotRootRead(), StringComparison.OrdinalIgnoreCase);

    public static string LSOptionsWorkspaceResolve(string lFolder, out bool lOccupied)
    {
        string lCurrent = LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder;
        string lNext = LDepot.LDepotRootResolve(lFolder);
        lOccupied = !string.Equals(lNext, LDepot.LDepotRootResolve(lCurrent), StringComparison.OrdinalIgnoreCase)
            && LDepot.LDepotOccupiedCheck(lNext);
        return lOccupied ? lCurrent : lFolder;
    }

    public static string LSOptionsLevelTitle => LLocalization.LLocalizationTextRead("Options.Work.Priority");

    public static string[] LSOptionsLevelNames =>
    [
        LLocalization.LLocalizationTextRead("Options.Work.PriorityLow"),
        LLocalization.LLocalizationTextRead("Options.Work.PriorityNormal"),
        LLocalization.LLocalizationTextRead("Options.Work.PriorityHigh")
    ];

    public IReadOnlyList<LSOptionsLevel> LSOptionsLevelsRead() =>
    [
        LSOptionsLevelCreate(LCustodyFamily.LCustodyFamilyEncode, "Options.Work.PriorityEncode"),
        LSOptionsLevelCreate(LCustodyFamily.LCustodyFamilyKeyframe, "Options.Work.PriorityKeyframe"),
        LSOptionsLevelCreate(LCustodyFamily.LCustodyFamilyWaveform, "Options.Work.PriorityWaveform")
    ];

    private LSOptionsLevel LSOptionsLevelCreate(LCustodyFamily lFamily, string lKey) =>
        new(
            LLocalization.LLocalizationTextRead(lKey),
            lsOptionsDraft.LPreferenceLevelRead(lFamily),
            lLevel => lsOptionsDraft.LPreferenceLevelSet(lFamily, lLevel));

    public bool LSOptionsRecordCheck() => lsOptionsDraft.LPreferenceRecordWorkspace;

    public string LSOptionsRecordResolve() => lsOptionsRecordKeys[LSOptionsRecordCheck()];

    public static string LSOptionsSidecarRead() => LSidecarStore.LSidecarFolderResolve();

    public bool LSOptionsApply()
    {
        bool lSaved = LPreference.LPreferenceStateSet(lsOptionsDraft.LPreferenceClone());
        LRenderer.LRendererEngineSet(
            string.Equals(lsOptionsDraft.LPreferencePreviewEngine, "Mpv", StringComparison.Ordinal)
                ? LPreviewEngine.LPreviewEngineMpv
                : LPreviewEngine.LPreviewEngineFlyleaf);
        return lSaved;
    }

    public bool LSOptionsRestartCheck() =>
        !string.Equals(
            LLocalization.LLocalizationLanguageNormalize(LPreference.LPreferenceStateCurrent.LPreferenceLanguage),
            LLocalization.LLocalizationLanguageRead(),
            StringComparison.OrdinalIgnoreCase);
}
