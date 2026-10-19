using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

// One tab's driver: builds the tab's drivers by its key, keeps its preset in step with the preset owner, and attaches
// and closes them with the tab.
public sealed class QWorkspace
{
    private readonly QWorkspacePair? qWorkspacePair;
    private Func<LSceneTabEntry>? qWorkspaceLayoutSource;

    // The tab hands its own workspace gate; every other gate is read from the atelier by the driver that uses it.
    public QWorkspace(string lKey, CWorkspace lWorkspace, CAtelier lAtelier)
    {
        CUsher lUsher = lAtelier.CAtelierUsher;
        QWorkspaceKey = lKey;
        QWorkspaceFacts = new QWorkspaceFacts(lKey);
        QWorkspaceRoster = QWorkspaceFacts.QWorkspaceWorklist
            ? QRoster.QRosterCreate(lAtelier)
            : null;
        QWorkspaceList = QWorkspaceFacts.QWorkspaceListPresent ? new QList(lAtelier) : null;
        qWorkspacePair = QWorkspaceFacts.QWorkspaceFlowPresent
            ? new QWorkspacePair(QWorkspaceFacts, lUsher, lAtelier.CAtelierInfo, lWorkspace)
            : null;
        QWorkspacePreset = LPresetTable.LPresetInitialCreate(lKey);
        QWorkspacePresetOwner = new LPresetSelection(string.Empty, lKey);
        QWorkspacePresetAttach();
        QWorkspacePresetAccept(QWorkspacePreset);
        QWorkspaceExport = new QExport(lUsher, lAtelier.CAtelierEncoder, QWorkspacePresetOwner, QWorkspaceFacts);
        QWorkspaceMode = new QWorkspaceMode(
            QWorkspaceFacts, lAtelier, QWorkspacePresetOwner, QWorkspaceViewer, QWorkspaceList, QWorkspaceFlow);
        QWorkspaceRelay = new QWorkspaceRelay(this);
        QWorkspaceMedia = new QWorkspaceMedia(this, lWorkspace);
        QWorkspaceHistory = new QWorkspaceHistory(this);
    }

    public string QWorkspaceKey { get; }

    public QWorkspaceMedia QWorkspaceMedia { get; }

    public QWorkspaceHistory QWorkspaceHistory { get; }

    public QWorkspaceFacts QWorkspaceFacts { get; }

    public QWorkspaceMode QWorkspaceMode { get; }

    public QWorkspaceRelay QWorkspaceRelay { get; }

    public LPreset QWorkspacePreset { get; }

    public LPresetSelection QWorkspacePresetOwner { get; }

    public QRoster? QWorkspaceRoster { get; }

    public QExport QWorkspaceExport { get; }

    public QList? QWorkspaceList { get; }

    public LDocket? QWorkspaceDocket => QWorkspaceList?.QListDocket;

    public QViewer? QWorkspaceViewer => qWorkspacePair?.QWorkspaceViewer;

    public QFlow? QWorkspaceFlow => qWorkspacePair?.QWorkspaceFlow;

    public QCompass? QWorkspaceCompass => qWorkspacePair?.QWorkspaceCompass;

    public QSource? QWorkspaceSource => qWorkspacePair?.QWorkspaceSource;

    public QInfo? QWorkspaceInfo => qWorkspacePair?.QWorkspaceInfo;

    public bool QWorkspaceFlowPresent => qWorkspacePair is not null;

    internal LSegment? QWorkspaceSegment => QWorkspaceFlow?.QFlowSection.QFlowSegment;

    public QWorkspaceFrame? QWorkspaceFrame { get; private set; }

    public FrameworkElement? QWorkspaceView => QWorkspaceFrame?.QWorkspaceFrameRoot;

    public void QWorkspaceFrameBuild(
        FrameworkElement lSurface,
        Func<UserControl> lFrameFactory,
        Func<QInfo, UIElement> lInfoFactory,
        Action lExportHandler,
        Func<ImageSource> lExportIcon,
        Action lSurfaceClose,
        Action lFlowClose,
        Action lViewerClose) =>
        QWorkspaceFrame = new QWorkspaceFrame(
            this, lSurface, lFrameFactory, lInfoFactory, lExportHandler, lExportIcon,
            lSurfaceClose, lFlowClose, lViewerClose);

    public void QWorkspaceFlowApply() => qWorkspacePair?.QWorkspaceFlowApply();

    public void QWorkspaceViewerApply() => qWorkspacePair?.QWorkspaceViewerApply();

    public void QWorkspaceCommandReset() => qWorkspacePair?.QWorkspaceCommandReset();

    public void QWorkspaceAttach(Func<LSceneTabEntry> lLayoutSource)
    {
        QWorkspaceHandlersAttach(lLayoutSource);
        QWorkspaceHistory.QWorkspaceHistoryAttach();
    }

    private void QWorkspacePresetAttach() => QWorkspacePresetOwner.LPresetSelectionChanged += QWorkspacePresetHandle;

    private void QWorkspaceHandlersAttach(Func<LSceneTabEntry> lLayoutSource)
    {
        qWorkspaceLayoutSource = lLayoutSource;
        if (QWorkspaceViewer is { } lViewer)
        {
            lViewer.QViewerMediaChanged += QWorkspaceMedia.QWorkspaceMediaHandle;
        }

        if (QWorkspaceFlow is { } lFlow)
        {
            lFlow.QFlowSection.QFlowSectionChanged += QWorkspaceHistory.QWorkspaceSectionHandle;
            lFlow.QFlowMediaChanged += QWorkspaceHistory.QWorkspaceHistoryReset;
        }

        if (QWorkspaceList is { } lList)
        {
            lList.QListPathChanged += QWorkspaceMedia.QWorkspacePathHandle;
            lList.QListClearChanged += QWorkspaceMedia.QWorkspaceRemovedHandle;
        }

        qWorkspacePair?.QWorkspacePairAttach();
    }

    public void QWorkspaceClose()
    {
        try
        {
            QWorkspaceHandlersDetach();
            QWorkspaceRelay.QWorkspaceRelayClose();
            QWorkspacePresetDetach();
            QWorkspacePresetOwner.LPresetSelectionClose();
        }
        finally
        {
            QWorkspaceRoster?.QRosterClose();
        }
    }

    private void QWorkspaceHandlersDetach()
    {
        if (QWorkspaceViewer is not null)
        {
            QWorkspaceViewer.QViewerMediaChanged -= QWorkspaceMedia.QWorkspaceMediaHandle;
        }

        if (QWorkspaceFlow is not null)
        {
            QWorkspaceFlow.QFlowSection.QFlowSectionChanged -= QWorkspaceHistory.QWorkspaceSectionHandle;
            QWorkspaceFlow.QFlowMediaChanged -= QWorkspaceHistory.QWorkspaceHistoryReset;
        }

        qWorkspacePair?.QWorkspacePairDetach();
    }

    private void QWorkspacePresetDetach()
    {
        QWorkspaceHistory.QWorkspaceHistoryDetach();
        QWorkspacePresetOwner.LPresetSelectionChanged -= QWorkspacePresetHandle;
    }

    public LStation? QWorkspaceStation => QWorkspaceRoster?.QRosterStation;

    public bool QWorkspaceBusyCheck() => QWorkspaceStation?.LStationBusyCheck() == true;

    public LSceneTabEntry QWorkspaceLayoutRead() => qWorkspaceLayoutSource?.Invoke() ?? new LSceneTabEntry();

    public void QWorkspacePresetAccept(LPreset lPreset)
    {
        QWorkspacePresetOwner.LPresetSelectionAccept(lPreset.LPresetRecordCreate(), lPreset.LPresetName);
        QWorkspacePresetHandle();
    }

    private void QWorkspacePresetHandle() =>
        QWorkspacePreset.LPresetCopy(LPreset.LPresetStateCreate(QWorkspacePresetOwner.LPresetSelectionValue));
}
