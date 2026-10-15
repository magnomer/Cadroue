using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public enum QViewerAskKind
{
    QViewerAskWarning,
    QViewerAskDecision,
}

public sealed record QViewerAsk(
    QViewerAskKind QViewerAskKind,
    string QViewerAskTitle,
    string QViewerAskMessage,
    string QViewerAskPrimary,
    string QViewerAskDismiss,
    string QViewerAskName,
    string QViewerAskFilter);

public sealed class QViewerSource
{
    private readonly QViewer qViewer;
    private readonly CUsher qViewerUsher;

    public QViewerSource(QViewer lOwner, CUsher lUsher)
    {
        qViewer = lOwner;
        qViewerUsher = lUsher;
        qViewer.QViewerOpenRequest += QViewerSourceOpen;
    }

    public event Action<QViewerAsk, Action<bool>>? QViewerSourceAsk;
    public event Action<QViewerAsk, Action<string?>>? QViewerSourceLocate;
    public event Action<IReadOnlyList<string>>? QViewerPathsDrop;

    private bool QViewerCropPersistent => qViewer.QCrop.QCropPersistent;

    public void QViewerSourceDetach() => qViewer.QViewerOpenRequest -= QViewerSourceOpen;

    public void QViewerSourceOpen(string lSourcePath)
    {
        LTraceLog.LTraceInfoRecord(
            $"Viewer source open requested '{System.IO.Path.GetFileName(lSourcePath)}'",
            $"command={(qViewer.QViewerCommandActive ? "active" : "INACTIVE")}, unloaded={qViewer.QViewerUnloaded}, "
            + $"engine={qViewer.QViewerEngine}, path={lSourcePath}");

        if (!qViewer.QViewerCommandActive || string.IsNullOrWhiteSpace(lSourcePath))
        {
            string lRefusal = qViewer.QViewerCommandActive
                ? "empty path"
                : "viewer command inactive (tab not the front workspace)";
            LTraceLog.LTraceWarningRecord($"Viewer source open refused: {lRefusal}");
            return;
        }

        qViewer.QViewerWorkspace.CWorkspaceRequestSet(lSourcePath);
        if (LLibrarian.LLibrarianFileCheck(lSourcePath))
        {
            if (QViewerSidecarResolve(lSourcePath) is not { } lResolvedPath)
            {
                LTraceLog.LTraceWarningRecord(
                    "Viewer source open refused: sidecar (.cad) source could not be resolved");
                return;
            }

            lSourcePath = lResolvedPath;
        }

        LPreference.LPreferenceMediaSet(lSourcePath);
        if (!QViewerCropPersistent)
        {
            qViewer.QViewerPreviewSet(qViewer.QViewerPreview.LRotateFlipChange(LRotateFlip.LRotateDefaultCreate()));
        }

        _ = qViewer.QViewerMedia.QViewerLoadStart(lSourcePath);
    }

    private string? QViewerSidecarResolve(string lSidecarPath)
    {
        LSidecarSourceResult? lResult = LLibrarian.LLibrarianSourceResolve(lSidecarPath);
        if (lResult is null)
        {
            QViewerAskRaise(QViewerAskCreate(
                QViewerAskKind.QViewerAskWarning,
                QLocalizationCatalog.QLocalizationTextRead("Viewer.Sidecar.ReadError")));
            return null;
        }

        if (lResult.LSidecarResultVerified)
        {
            return lResult.LSidecarResultPath;
        }

        if (lResult.LSidecarResultKind != LSidecarSourceKind.LSidecarSourceMissing
            && QViewerAskRaise(QViewerAskCreate(
                QViewerAskKind.QViewerAskDecision,
                QLocalizationCatalog.QLocalizationFormat("Viewer.Sidecar.MismatchFound", lResult.LSidecarResultPath))))
        {
            return lResult.LSidecarResultPath;
        }

        return QViewerSidecarFind(lSidecarPath, lResult.LSidecarResultName);
    }

    private string? QViewerSidecarFind(string lSidecarPath, string lSidecarName)
    {
        string? lChosen = null;
        QViewerSourceLocate?.Invoke(
            new QViewerAsk(
                QViewerAskKind.QViewerAskDecision,
                QLocalizationCatalog.QLocalizationFormat("Viewer.Locate.Title", lSidecarName),
                string.Empty,
                string.Empty,
                string.Empty,
                lSidecarName,
                QLocalizationCatalog.QLocalizationTextRead("Viewer.Dialog.MediaFilter")),
            lPath => lChosen = lPath);
        if (lChosen is null)
        {
            return null;
        }

        if (LLibrarian.LLibrarianSourceMatch(lChosen, lSidecarPath))
        {
            return lChosen;
        }

        return QViewerAskRaise(QViewerAskCreate(
            QViewerAskKind.QViewerAskDecision,
            QLocalizationCatalog.QLocalizationTextRead("Viewer.Sidecar.MismatchSelected")))
            ? lChosen
            : null;
    }

    private static QViewerAsk QViewerAskCreate(QViewerAskKind lKind, string lMessage) =>
        new(
            lKind,
            QLocalizationCatalog.QLocalizationTextRead("Viewer.Dialog.OpenTitle"),
            lMessage,
            QLocalizationCatalog.QLocalizationTextRead("Terms.Open"),
            QLocalizationCatalog.QLocalizationTextRead("Terms.Cancel"),
            string.Empty,
            string.Empty);

    private bool QViewerAskRaise(QViewerAsk lAsk)
    {
        bool lAnswer = false;
        QViewerSourceAsk?.Invoke(lAsk, lChoice => lAnswer = lChoice);
        return lAnswer;
    }

    public QWindowDropEffect QViewerDropResolve(string[]? lPaths, bool lCopyable)
    {
        IReadOnlyList<string> lList = lPaths ?? [];
        if (QViewerPathsDrop is not null)
        {
            return QWindowDrop.QWindowMediaCheck(qViewerUsher, lList) && lCopyable
                ? QWindowDropEffect.QWindowDropFile
                : QWindowDropEffect.QWindowDropNone;
        }

        string? lSourcePath = QWindowDrop.QWindowFileFind(qViewerUsher, lList);
        if (lSourcePath is null || (LMedia.LMediaAudioCheck(lSourcePath) && !qViewer.QViewerAudioAllowed))
        {
            return QWindowDropEffect.QWindowDropNone;
        }

        return lCopyable ? QWindowDropEffect.QWindowDropFile : QWindowDropEffect.QWindowDropNone;
    }

    public QWindowDropEffect QViewerDropHandle(string[]? lPaths, bool lCopyable)
    {
        QWindowDropEffect lEffect = QViewerDropResolve(lPaths, lCopyable);
        if (lEffect == QWindowDropEffect.QWindowDropNone)
        {
            return lEffect;
        }

        IReadOnlyList<string> lList = lPaths ?? [];
        if (QViewerPathsDrop is not null)
        {
            QViewerPathsDrop.Invoke(lList);
            return lEffect;
        }

        string? lSourcePath = QWindowDrop.QWindowFileFind(qViewerUsher, lList);
        if (lSourcePath is null)
        {
            return QWindowDropEffect.QWindowDropNone;
        }

        QViewerSourceOpen(lSourcePath);
        return lEffect;
    }

}
