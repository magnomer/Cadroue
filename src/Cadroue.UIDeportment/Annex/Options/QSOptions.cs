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
    private readonly CSOptions csOptions;
    private QSOptionsPage qsOptionsPage;

    public QSOptions(CUsher lUsher)
        : this(lUsher, LPreference.LPreferenceStateCurrent.LPreferenceClone(), LMpvAvailability.LMpvAvailableCheck())
    {
    }

    public QSOptions(CUsher lUsher, LPreferenceState lDraft, bool lMpvEnabled)
    {
        csOptions = new CSOptions(lUsher, lDraft, LSOptions.LSOptionsRead, LSOptions.LSOptionsApply);
        QSWorkspace = new QSWorkspace(csOptions);
        QSFlyleaf = new QSFlyleaf();
        QSMpv = new QSMpv(lMpvEnabled);
        LOptionsSnapshot snapshot = ((LOptionsPort)csOptions).CSOptionsSnapshotRead();
        QSOptionsGeneral = new QSGeneral(
            new QSGeneralSnapshot(
                snapshot.CSOptionsStartup,
                snapshot.CSOptionsSelection,
                snapshot.CSOptionsRecent,
                snapshot.CSOptionsSafety,
                snapshot.CSOptionsVacancy,
                snapshot.CSOptionsOrientation,
                snapshot.CSOptionsLanguage),
            csOptions);
        QSRenderer = new QSRenderer(snapshot.CSOptionsRenderer);
    }

    public QSOptions(
        CUsher lUsher,
        LPreferenceState lDraft,
        bool lMpvEnabled,
        CSFlyleafPort flyleaf,
        CSMpvPort mpv)
    {
        csOptions = new CSOptions(lUsher, lDraft, LSOptions.LSOptionsRead, LSOptions.LSOptionsApply);
        QSWorkspace = new QSWorkspace(csOptions);
        QSFlyleaf = new QSFlyleaf(flyleaf);
        QSMpv = new QSMpv(lMpvEnabled, mpv);
        LOptionsSnapshot snapshot = ((LOptionsPort)csOptions).CSOptionsSnapshotRead();
        QSOptionsGeneral = new QSGeneral(
            new QSGeneralSnapshot(
                snapshot.CSOptionsStartup,
                snapshot.CSOptionsSelection,
                snapshot.CSOptionsRecent,
                snapshot.CSOptionsSafety,
                snapshot.CSOptionsVacancy,
                snapshot.CSOptionsOrientation,
                snapshot.CSOptionsLanguage),
            csOptions);
        QSRenderer = new QSRenderer(snapshot.CSOptionsRenderer);
    }

    public LPreferenceState QSOptionsDraft =>
        ((LOptionsPort)csOptions).CSOptionsDraftRead<LPreferenceState>();

    public QSOptionsPage QSOptionsPage => qsOptionsPage;

    public QSFlyleaf QSFlyleaf { get; }

    public QSMpv QSMpv { get; }

    public QSWorkspace QSWorkspace { get; }

    public QSGeneral QSOptionsGeneral { get; }

    public QSRenderer QSRenderer { get; }

    public void QSOptionsPageSelect(QSOptionsPage lPage) => qsOptionsPage = lPage;

    public QSDiagnosis QSOptionsDiagnosisCreate() => new();

    public QSSpectrum QSOptionsSpectrumCreate() =>
        new(
            ((LOptionsPort)csOptions).CSOptionsSnapshotRead().CSOptionsPalette,
            ((LOptionsPort)csOptions).CSOptionsPaletteRead);

    public bool QSOptionsFolderExist(string lFolder) =>
        ((LOptionsPort)csOptions).CSOptionsFolderExist(lFolder);

    public string? QSOptionsFolderOpen(string lFolder, string lFallback) =>
        ((LOptionsPort)csOptions).CSOptionsFolderOpen(lFolder, lFallback);

    public string QSOptionsEngineRead() =>
        ((LOptionsPort)csOptions).CSOptionsEngineRead()(QSMpv.QSMpvEnabled);

    public static string QSOptionsLevelTitle => QLocalizationCatalog.QLocalizationTextRead("Options.Work.Priority");

    public static string[] QSOptionsLevelNames =>
    [
        QLocalizationCatalog.QLocalizationTextRead("Options.Work.PriorityLow"),
        QLocalizationCatalog.QLocalizationTextRead("Options.Work.PriorityNormal"),
        QLocalizationCatalog.QLocalizationTextRead("Options.Work.PriorityHigh")
    ];

    public IReadOnlyList<QSOptionsLevel> QSOptionsLevelsRead()
    {
        LOptionsSnapshot snapshot = ((LOptionsPort)csOptions).CSOptionsSnapshotRead();
        return
        [
            QSOptionsLevelCreate(
                LCustodyFamily.LCustodyFamilyEncode,
                "Options.Work.PriorityEncode",
                snapshot.CSOptionsEncodeLevel),
            QSOptionsLevelCreate(
                LCustodyFamily.LCustodyFamilyKeyframe,
                "Options.Work.PriorityKeyframe",
                snapshot.CSOptionsKeyframeLevel),
            QSOptionsLevelCreate(
                LCustodyFamily.LCustodyFamilyWaveform,
                "Options.Work.PriorityWaveform",
                snapshot.CSOptionsWaveformLevel)
        ];
    }

    private QSOptionsLevel QSOptionsLevelCreate(LCustodyFamily lFamily, string lKey, int lLevel) =>
        new(
            QLocalizationCatalog.QLocalizationTextRead(lKey),
            lLevel,
            lChoice => ((LOptionsPort)csOptions).CSOptionsLevelSet(lFamily, lChoice));

    public string QSOptionsRecordResolve() => ((LOptionsPort)csOptions).CSOptionsRecordResolve();

    public static string QSOptionsSidecarRead()
    {
        LOptionsPort options = new CSOptions(
            null!,
            LPreference.LPreferenceStateCurrent.LPreferenceClone(),
            LSOptions.LSOptionsRead,
            LSOptions.LSOptionsApply);
        return options.CSOptionsSidecarRead();
    }

    public bool QSOptionsApply()
    {
        _ = QSOptionsGeneral.QSGeneralSnapshotRead();
        return ((LOptionsPort)csOptions).CSOptionsApply().CSOptionsSaved;
    }

    public bool QSOptionsRestartCheck() => ((LOptionsPort)csOptions).CSOptionsRestartCheck();

}
