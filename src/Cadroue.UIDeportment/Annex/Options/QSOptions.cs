using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public enum QSOptionsPage
{
    QSOptionsPageGeneral,
    QSOptionsPageSystem,
    QSOptionsPagePlayback,
    QSOptionsPageTimeline,
    QSOptionsPageWork
}

public delegate Border QSOptionsMode(
    string selected,
    Action change,
    out Action<string, bool> enable,
    params LLocalizationChoice[] choices);

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
                csOptions.CSOptionsLanguageResolve()),
            csOptions);
        QSRenderer = new QSRenderer(snapshot.CSOptionsRenderer);
        QSOptionsSystem = new QSOptionsSystem(csOptions, QSWorkspace, QSRenderer);
        QSOptionsPlayback = new QSOptionsPlayback(csOptions, QSMpv);
        QSOptionsTimeline = new QSOptionsTimeline(csOptions);
        QSOptionsWork = new QSOptionsWork(csOptions);
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
                csOptions.CSOptionsLanguageResolve()),
            csOptions);
        QSRenderer = new QSRenderer(snapshot.CSOptionsRenderer);
        QSOptionsSystem = new QSOptionsSystem(csOptions, QSWorkspace, QSRenderer);
        QSOptionsPlayback = new QSOptionsPlayback(csOptions, QSMpv);
        QSOptionsTimeline = new QSOptionsTimeline(csOptions);
        QSOptionsWork = new QSOptionsWork(csOptions);
    }

    public QSOptionsPage QSOptionsPage => qsOptionsPage;

    public QSFlyleaf QSFlyleaf { get; }

    public QSMpv QSMpv { get; }

    public QSWorkspace QSWorkspace { get; }

    public QSGeneral QSOptionsGeneral { get; }

    public QSRenderer QSRenderer { get; }

    public QSOptionsPlayback QSOptionsPlayback { get; }

    public QSOptionsTimeline QSOptionsTimeline { get; }

    public QSOptionsWork QSOptionsWork { get; }

    public QSOptionsSystem QSOptionsSystem { get; }

    public void QSOptionsPageSelect(QSOptionsPage lPage)
    {
        if (lPage >= QSOptionsPage.QSOptionsPageGeneral)
        {
            qsOptionsPage = lPage;
        }
    }

    public QSDiagnosis QSOptionsDiagnosisCreate() => new();

    public QSSpectrum QSOptionsSpectrumCreate() =>
        new(
            ((LOptionsPort)csOptions).CSOptionsSnapshotRead().CSOptionsPalette,
            ((LOptionsPort)csOptions).CSOptionsPaletteRead);

    public string? QSOptionsFolderOpen(string lFolder, string lFallback) =>
        ((LOptionsPort)csOptions).CSOptionsFolderOpen(lFolder, lFallback);

    public static string QSOptionsLevelTitle => QLocalizationCatalog.QLocalizationTextRead("Options.Work.Priority");

    public static string[] QSOptionsLevelNames =>
    [
        QLocalizationCatalog.QLocalizationTextRead("Options.Work.PriorityLow"),
        QLocalizationCatalog.QLocalizationTextRead("Options.Work.PriorityNormal"),
        QLocalizationCatalog.QLocalizationTextRead("Options.Work.PriorityHigh")
    ];

    public void QSOptionsCommit()
    {
        csOptions.CSOptionsSheetCommit(
            QSOptionsPlayback.QSOptionsPlaybackRead(),
            QSOptionsTimeline.QSOptionsTimelineRead(),
            QSOptionsWork.QSOptionsWorkRead(),
            QSOptionsSystem.QSOptionsSystemRead());
        QSWorkspace.QSWorkspaceCommit();
    }

    public void QSOptionsDetach()
    {
        QSOptionsPlayback.QSOptionsPlaybackDetach();
        QSOptionsTimeline.QSOptionsTimelineDetach();
        QSOptionsWork.QSOptionsWorkDetach();
        QSOptionsSystem.QSOptionsSystemDetach();
    }

    public bool QSOptionsApply()
    {
        _ = QSOptionsGeneral.QSGeneralSnapshotRead();
        return ((LOptionsPort)csOptions).CSOptionsApply().CSOptionsSaved;
    }

    public bool QSOptionsRestartCheck() => ((LOptionsPort)csOptions).CSOptionsRestartCheck();

}
