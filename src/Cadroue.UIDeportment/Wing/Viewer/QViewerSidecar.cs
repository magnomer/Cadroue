using Cadroue.Application;
using Cadroue.Core;

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

// The questions a sidecar's open puts to the user, and the media path their answers settle.
public sealed class QViewerSidecar
{
    public event Action<QViewerAsk, Action<bool>>? QViewerSourceAsk;
    public event Action<QViewerAsk, Action<string?>>? QViewerSourceLocate;

    // A sidecar (.cad) path resolves to its media through the ask and locate chain; any other path loads as given.
    public string? QViewerSourceResolve(string lPath) =>
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
}
