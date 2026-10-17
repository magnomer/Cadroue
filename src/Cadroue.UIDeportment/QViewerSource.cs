using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
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
        QViewerSourceAttach();
    }

    public event Action<QViewerAsk, Action<bool>>? QViewerSourceAsk;
    public event Action<QViewerAsk, Action<string?>>? QViewerSourceLocate;
    public event Action<IReadOnlyList<string>>? QViewerPathsDrop;

    private bool QViewerCropPersistent => qViewer.QCrop.QCropPersistent;

    private void QViewerSourceAttach() => qViewer.QViewerOpenRequest += QViewerSourceOpen;

    public void QViewerSourceDetach() => qViewer.QViewerOpenRequest -= QViewerSourceOpen;

    public void QViewerSourceOpen(string lSourcePath)
    {
        if (qViewer.QViewerWorkspace.CWorkspaceRequestResolve(
                lSourcePath,
                qViewer.QViewerTrace.CViewerCommandActive,
                qViewer.QViewerTrace.CViewerUnloaded,
                qViewer.QViewerEngine.ToString(),
                QViewerSourceResolve) is not { } lLoadPath)
        {
            return;
        }

        lSourcePath = lLoadPath;
        LPreference.LPreferenceMediaSet(lSourcePath);
        if (!QViewerCropPersistent)
        {
            qViewer.QViewerPreviewSet(qViewer.QViewerPreview.LRotateFlipChange(LRotateFlip.LRotateDefaultCreate()));
        }

        _ = qViewer.QViewerMedia.QViewerLoadStart(lSourcePath);
    }

    // A sidecar (.cad) path resolves to its media through the ask and locate chain; any other path loads as given.
    private string? QViewerSourceResolve(string lPath) =>
        LLibrarian.LLibrarianFileCheck(lPath) ? QViewerSidecarResolve(lPath) : lPath;

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
