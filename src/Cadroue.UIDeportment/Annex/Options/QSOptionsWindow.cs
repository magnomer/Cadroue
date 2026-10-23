using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QSOptionsWindow
{
    public const string QSOptionsWindowPlacement = "Options";

    private const double QSOptionsWindowTab = 112;
    private const double QSOptionsWindowStrip = QSOptionsWindowTab * 5;

    private readonly QSOptions qsOptions;
    private readonly QSOptionsAnswer qsOptionsWindowAnswer;
    private readonly Window psOptions;
    private readonly TabControl psOptionsDeck;
    private readonly (Action qsOptionsWindowAttachment, Action qsOptionsWindowDetachment) qsOptionsWindowGrabber;
    private readonly QSWorkspaceAnswer qsOptionsWindowWorkspace;
    private readonly QSRendererAnswer qsOptionsWindowRenderer;
    private readonly QSFlyleafAnswer qsOptionsWindowFlyleaf;
    private readonly QSMpvAnswer qsOptionsWindowMpv;
    private readonly Action? qsOptionsWindowApplied;
    private QSSpectrum? qsOptionsWindowSpectrum;
    private QSSpectrumDialog? qsSpectrumDialog;

    private QSOptionsWindow(QSOptions options, QSOptionsAnswer answer, Action? applied)
    {
        qsOptions = options;
        qsOptionsWindowAnswer = answer;
        qsOptionsWindowApplied = applied;
        psOptions = answer.QSOptionsSurfaceBuild();
        psOptionsDeck = QContract.QContractFind<TabControl>(psOptions, "PSOptionsDeck");
        FrameworkElement body = QContract.QContractFind<FrameworkElement>(psOptions, "PSOptionsBody");
        psOptions.Content = null;
        psOptions.Content = answer.QSOptionsShellBuild(psOptions, QSOptionsWindowStrip, body);
        answer.QSOptionsDeckApply(psOptionsDeck, QSOptionsWindowTab);
        ((TabItem)psOptionsDeck.Items[0]).Header = answer.QSOptionsHeaderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.General"), "/PAsset/PTab/PSSheetGeneral.svg");
        ((TabItem)psOptionsDeck.Items[1]).Header = answer.QSOptionsHeaderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.System"), "/PAsset/PTab/PSSheetSystem.svg");
        ((TabItem)psOptionsDeck.Items[2]).Header = answer.QSOptionsHeaderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.Playback"), "/PAsset/PTab/PSSheetPlayback.svg");
        ((TabItem)psOptionsDeck.Items[3]).Header = answer.QSOptionsHeaderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.Timeline"), "/PAsset/PTab/PSSheetTimeline.svg");
        ((TabItem)psOptionsDeck.Items[4]).Header = answer.QSOptionsHeaderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Sheet.Work"), "/PAsset/PTab/PSSheetWork.svg");
        qsOptionsWindowGrabber = answer.QSOptionsGrabberBuild(psOptions);
        qsOptionsWindowWorkspace = new QSWorkspaceAnswer(
            psOptions, answer.QSOptionsConfirmationShow, answer.QSOptionsWarningShow, answer.QSOptionsAnnouncementShow);
        qsOptionsWindowRenderer = new QSRendererAnswer(qsOptions, psOptions, answer.QSOptionsDiagnosisShow);
        qsOptionsWindowFlyleaf = new QSFlyleafAnswer(
            qsOptions, psOptions, answer.QSOptionsAnnouncementShow, answer.QSOptionsWarningShow);
        qsOptionsWindowMpv = new QSMpvAnswer(
            qsOptions, psOptions, answer.QSOptionsAnnouncementShow, answer.QSOptionsWarningShow);
    }

    public static void QSOptionsWindowShow(QSOptions options, QSOptionsAnswer answer, Window owner, Action? applied) =>
        new QSOptionsWindow(options, answer, applied).QSOptionsWindowShow(owner);

    private void QSOptionsWindowShow(Window owner)
    {
        psOptions.Title = QLocalizationCatalog.QLocalizationTextRead("Options.Window.Title");
        psOptions.Owner = owner;
        QSOptionsWindowAttach();
        qsOptionsWindowAnswer.QSOptionsPlacementRestore(psOptions, QSOptionsWindowPlacement);
        qsOptionsWindowGrabber.qsOptionsWindowAttachment();
        psOptions.Closed += QSOptionsWindowDetach;
        psOptions.ShowDialog();
    }

    private void QSOptionsWindowAttach()
    {
        QSOptionsWindowAttach(qsOptions.QSOptionsTimeline);
        _ = qsOptions.QSOptionsGeneral.QSGeneralAttach(
            QContract.QContractFind<FrameworkElement>(psOptions, "PSGeneral"));
        QSOptionsWindowAttach(qsOptions.QSOptionsSystem);
        QSOptionsWindowAttach(qsOptions.QSOptionsPlayback);
        _ = qsOptions.QSOptionsWork.QSOptionsWorkAttach(
            QContract.QContractFind<FrameworkElement>(psOptions, "PSOptionsWork"));

        Button applier = QContract.QContractFind<Button>(psOptions, "PSOptionsApplier");
        Button confirmer = QContract.QContractFind<Button>(psOptions, "PSOptionsConfirmer");
        Button canceller = QContract.QContractFind<Button>(psOptions, "PSOptionsCanceller");
        applier.Content = QLocalizationCatalog.QLocalizationTextRead("Options.Button.Apply");
        confirmer.Content = QLocalizationCatalog.QLocalizationTextRead("Options.Button.OK");
        canceller.Content = QLocalizationCatalog.QLocalizationTextRead("Options.Button.Cancel");
        QButton.QButtonBuild(applier, QButtonLook.QButtonLookWhite);
        QButton.QButtonBuild(confirmer, QButtonLook.QButtonLookWhite);
        QButton.QButtonBuild(canceller, QButtonLook.QButtonLookWhite);
        applier.Click += QSOptionsWindowApply;
        confirmer.Click += QSOptionsWindowConfirm;
        canceller.Click += QSOptionsWindowCancel;
        psOptionsDeck.SelectionChanged += QSOptionsWindowSelect;
    }

    private void QSOptionsWindowAttach(QSOptionsTimeline timeline)
    {
        qsOptionsWindowSpectrum = qsOptions.QSOptionsSpectrumCreate();
        FrameworkElement palette = QContract.QContractSheetFind<FrameworkElement>(psOptions, "PSSpectrum");
        qsSpectrumDialog = new QSSpectrumDialog(
            qsOptionsWindowSpectrum,
            palette,
            message => qsOptionsWindowAnswer.QSOptionsWarningShow(
                psOptions,
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.LoadTitle"),
                QLocalizationCatalog.QLocalizationTextRead(message)),
            question => qsOptionsWindowAnswer.QSOptionsConfirmationShow(
                psOptions,
                QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.RemoveTitle"),
                question,
                QLocalizationCatalog.QLocalizationTextRead("Terms.Remove")));
        qsSpectrumDialog.QSSpectrumDialogAttach();
        _ = timeline.QSOptionsTimelineAttach(
            QContract.QContractFind<FrameworkElement>(psOptions, "PSOptionsTimeline"),
            palette,
            () => qsOptionsWindowSpectrum!.QSSpectrumName,
            qsOptionsWindowAnswer.QSOptionsModeRead(),
            qsOptionsWindowAnswer.QSOptionsChoiceRead);
    }

    private void QSOptionsWindowAttach(QSOptionsSystem system) =>
        _ = system.QSOptionsSystemAttach(
            QContract.QContractFind<FrameworkElement>(psOptions, "PSOptionsSystem"),
            qsOptionsWindowWorkspace,
            qsOptionsWindowRenderer,
            qsOptionsWindowAnswer.QSOptionsModeRead(),
            qsOptionsWindowAnswer.QSOptionsChoiceRead);

    private void QSOptionsWindowAttach(QSOptionsPlayback playback) =>
        _ = playback.QSOptionsPlaybackAttach(
            QContract.QContractFind<FrameworkElement>(psOptions, "PSOptionsPlayback"),
            QSOptionsWindowAttach(qsOptions.QSFlyleaf),
            QSOptionsWindowAttach(qsOptions.QSMpv),
            qsOptionsWindowAnswer.QSOptionsModeRead(),
            qsOptionsWindowAnswer.QSOptionsChoiceRead);

    private FrameworkElement QSOptionsWindowAttach(QSFlyleaf flyleaf) =>
        flyleaf.QSFlyleafAttach(
            QContract.QContractSheetFind<FrameworkElement>(psOptions, "PSFlyleaf"),
            qsOptionsWindowFlyleaf);

    private FrameworkElement QSOptionsWindowAttach(QSMpv mpv) =>
        mpv.QSMpvAttach(QContract.QContractSheetFind<FrameworkElement>(psOptions, "PSMpv"), qsOptionsWindowMpv);

    private void QSOptionsWindowSelect(object sender, SelectionChangedEventArgs _) =>
        qsOptions.QSOptionsPageSelect((QSOptionsPage)psOptionsDeck.SelectedIndex);

    private void QSOptionsWindowApply(object sender, RoutedEventArgs _) => QSOptionsWindowApply();

    private void QSOptionsWindowConfirm(object sender, RoutedEventArgs _)
    {
        QSOptionsWindowApply();
        psOptions.Close();
    }

    private void QSOptionsWindowCancel(object sender, RoutedEventArgs _) => psOptions.Close();

    private void QSOptionsWindowApply()
    {
        qsOptions.QSOptionsCommit();
        QSOptionsWindowUpdate(qsOptions.QSOptionsApply());
    }

    private void QSOptionsWindowUpdate(bool saved)
    {
        qsOptionsWindowApplied?.Invoke();
        qsOptions.QSOptionsSystem.QSOptionsSystemUpdate();
        if (!saved)
        {
            qsOptionsWindowAnswer.QSOptionsWarningShow(
                psOptions,
                QLocalizationCatalog.QLocalizationTextRead("Options.Save.FailedTitle"),
                QLocalizationCatalog.QLocalizationTextRead("Options.Save.FailedMessage"));
            return;
        }

        if (qsOptions.QSOptionsRestartCheck())
        {
            qsOptionsWindowAnswer.QSOptionsAnnouncementShow(
                psOptions,
                QLocalizationCatalog.QLocalizationTextRead("Options.Language.RestartTitle"),
                QLocalizationCatalog.QLocalizationTextRead("Options.Language.RestartMessage"));
        }
    }

    private void QSOptionsWindowDetach(object? sender, EventArgs _)
    {
        qsSpectrumDialog!.QSSpectrumDialogDetach();
        qsOptions.QSWorkspace.QSWorkspaceDetach();
        qsOptions.QSOptionsDetach();
        qsOptions.QSOptionsGeneral.QSGeneralDetach();
        qsOptions.QSRenderer.QSRendererDetach();
        qsOptions.QSFlyleaf.QSFlyleafDetach();
        qsOptions.QSMpv.QSMpvDetach();
        qsOptionsWindowAnswer.QSOptionsPlacementSave(psOptions, QSOptionsWindowPlacement);
        qsOptionsWindowGrabber.qsOptionsWindowDetachment();
        QContract.QContractFind<Button>(psOptions, "PSOptionsApplier").Click -= QSOptionsWindowApply;
        QContract.QContractFind<Button>(psOptions, "PSOptionsConfirmer").Click -= QSOptionsWindowConfirm;
        QContract.QContractFind<Button>(psOptions, "PSOptionsCanceller").Click -= QSOptionsWindowCancel;
        psOptionsDeck.SelectionChanged -= QSOptionsWindowSelect;
        psOptions.Closed -= QSOptionsWindowDetach;
    }
}
