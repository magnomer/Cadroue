using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum QSOptionsPage
{
    QSOptionsPageGeneral,
    QSOptionsPageSystem,
    QSOptionsPagePlayback,
    QSOptionsPageTimeline,
    QSOptionsPageWork
}

public sealed record QSOptionsLevel(
    string QSOptionsLevelText,
    int QSOptionsLevelIndex,
    Action<int> QSOptionsLevelChoice);

public sealed class QSOptions
{
    private static readonly IReadOnlyDictionary<bool, string> qsOptionsRecordKeys = new Dictionary<bool, string>
    {
        [true] = "Options.System.FileRecordWorkspace",
        [false] = "Options.System.FileRecordBeside",
    };

    private readonly CUsher qsOptionsUsher;
    private readonly LPreferenceState qsOptionsDraft;
    private QSOptionsPage qsOptionsPage;
    private bool qsOptionsMpvEnabled;

    public event Action<bool>? QSOptionsMpvChanged;

    public QSOptions(CUsher lUsher)
        : this(lUsher, LPreference.LPreferenceStateCurrent.LPreferenceClone(), LMpv.LMpvAvailableCheck())
    {
    }

    public QSOptions(CUsher lUsher, LPreferenceState lDraft, bool lMpvEnabled)
    {
        qsOptionsUsher = lUsher;
        qsOptionsDraft = lDraft;
        qsOptionsMpvEnabled = lMpvEnabled;
    }

    public LPreferenceState QSOptionsDraft => qsOptionsDraft;

    public QSOptionsPage QSOptionsPage => qsOptionsPage;

    public bool QSOptionsMpvEnabled => qsOptionsMpvEnabled;

    public void QSOptionsPageSelect(QSOptionsPage lPage) => qsOptionsPage = lPage;

    public QSDiagnosis QSOptionsDiagnosisCreate() => new();

    public QSSpectrum QSOptionsSpectrumCreate() => new(qsOptionsDraft.LPreferenceSectionPalette);

    public bool QSOptionsFolderExist(string lFolder) => qsOptionsUsher.CUsherFolderExist(lFolder);

    public string? QSOptionsFolderOpen(string lFolder, string lFallback) =>
        qsOptionsUsher.CUsherFolderOpen(string.IsNullOrWhiteSpace(lFolder) ? lFallback : lFolder);

    public string QSOptionsEngineRead() =>
        qsOptionsMpvEnabled && string.Equals(qsOptionsDraft.LPreferencePreviewEngine, "Mpv", StringComparison.Ordinal)
            ? "Mpv"
            : "Flyleaf";

    public void QSOptionsMpvSet(bool lEnabled)
    {
        qsOptionsMpvEnabled = lEnabled;
        QSOptionsMpvChanged?.Invoke(lEnabled);
    }

    public async Task QSOptionsMpvUpdate()
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

        QSOptionsMpvSet(lUsable);
    }

    public static string QSOptionsMpvResolve() =>
        LMpv.LMpvInstalledCheck() ? "Options.System.ReinstallMpv" : "Options.System.DownloadMpv";

    public static string QSOptionsFlyleafResolve() =>
        LFlyleaf.LFlyleafInstalledCheck() ? "Options.System.ReinstallFlyleaf" : "Options.System.InstallFlyleaf";

    public static string QSOptionsFfmpegResolve(string lFolder)
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

    public static bool QSOptionsAppliedCheck(string lFolder) =>
        string.Equals(LDepot.LDepotRootResolve(lFolder), LDepot.LDepotRootRead(), StringComparison.OrdinalIgnoreCase);

    public static string QSOptionsWorkspaceResolve(string lFolder, out bool lOccupied)
    {
        string lCurrent = LPreference.LPreferenceStateCurrent.LPreferenceWorkspaceFolder;
        string lNext = LDepot.LDepotRootResolve(lFolder);
        lOccupied = !string.Equals(lNext, LDepot.LDepotRootResolve(lCurrent), StringComparison.OrdinalIgnoreCase)
            && LDepotRelocation.LDepotOccupiedCheck(lNext);
        return lOccupied ? lCurrent : lFolder;
    }

    public static string QSOptionsLevelTitle => QLocalizationCatalog.QLocalizationTextRead("Options.Work.Priority");

    public static string[] QSOptionsLevelNames =>
    [
        QLocalizationCatalog.QLocalizationTextRead("Options.Work.PriorityLow"),
        QLocalizationCatalog.QLocalizationTextRead("Options.Work.PriorityNormal"),
        QLocalizationCatalog.QLocalizationTextRead("Options.Work.PriorityHigh")
    ];

    public IReadOnlyList<QSOptionsLevel> QSOptionsLevelsRead() =>
    [
        QSOptionsLevelCreate(LCustodyFamily.LCustodyFamilyEncode, "Options.Work.PriorityEncode"),
        QSOptionsLevelCreate(LCustodyFamily.LCustodyFamilyKeyframe, "Options.Work.PriorityKeyframe"),
        QSOptionsLevelCreate(LCustodyFamily.LCustodyFamilyWaveform, "Options.Work.PriorityWaveform")
    ];

    private QSOptionsLevel QSOptionsLevelCreate(LCustodyFamily lFamily, string lKey) =>
        new(
            QLocalizationCatalog.QLocalizationTextRead(lKey),
            qsOptionsDraft.LPreferenceLevelRead(lFamily),
            lLevel => qsOptionsDraft.LPreferenceLevelSet(lFamily, lLevel));

    public bool QSOptionsRecordCheck() => qsOptionsDraft.LPreferenceRecordWorkspace;

    public string QSOptionsRecordResolve() => qsOptionsRecordKeys[QSOptionsRecordCheck()];

    public static string QSOptionsSidecarRead() => LSidecarPath.LSidecarFolderResolve();

    public bool QSOptionsApply()
    {
        bool lSaved = LPreference.LPreferenceStateSet(qsOptionsDraft.LPreferenceClone());
        LRenderer.LRendererEngineSet(
            string.Equals(qsOptionsDraft.LPreferencePreviewEngine, "Mpv", StringComparison.Ordinal)
                ? LPreviewEngine.LPreviewEngineMpv
                : LPreviewEngine.LPreviewEngineFlyleaf);
        return lSaved;
    }

    public bool QSOptionsRestartCheck() =>
        !string.Equals(
            LLocalization.LLocalizationLanguageNormalize(LPreference.LPreferenceStateCurrent.LPreferenceLanguage),
            LLocalization.LLocalizationLanguageRead(),
            StringComparison.OrdinalIgnoreCase);
}
