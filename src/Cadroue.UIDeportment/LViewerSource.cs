using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public enum LViewerAskKind
{
    LViewerAskWarning,
    LViewerAskDecision,
}

public sealed record LViewerAsk(
    LViewerAskKind LViewerAskKind,
    string LViewerAskTitle,
    string LViewerAskMessage,
    string LViewerAskPrimary,
    string LViewerAskDismiss,
    string LViewerAskName,
    string LViewerAskFilter);

public sealed class LViewerSource
{
    private readonly LViewer lViewer;

    public LViewerSource(LViewer lOwner)
    {
        lViewer = lOwner;
        lViewer.LViewerOpenRequest += LViewerSourceOpen;
    }

    public event Action<LViewerAsk, Action<bool>>? LViewerSourceAsk;
    public event Action<LViewerAsk, Action<string?>>? LViewerSourceLocate;
    public event Action<IReadOnlyList<string>>? LViewerPathsDrop;

    private bool LViewerCropPersistent => lViewer.LCrop.LCropPersistent;

    public void LViewerSourceDetach() => lViewer.LViewerOpenRequest -= LViewerSourceOpen;

    public void LViewerSourceOpen(string lSourcePath)
    {
        LTraceLog.LTraceInfoRecord(
            $"Viewer source open requested '{System.IO.Path.GetFileName(lSourcePath)}'",
            $"command={(lViewer.LViewerCommandActive ? "active" : "INACTIVE")}, unloaded={lViewer.LViewerUnloaded}, "
            + $"engine={lViewer.LViewerEngine}, path={lSourcePath}");

        if (!lViewer.LViewerCommandActive || string.IsNullOrWhiteSpace(lSourcePath))
        {
            string lRefusal = lViewer.LViewerCommandActive
                ? "empty path"
                : "viewer command inactive (tab not the front workspace)";
            LTraceLog.LTraceWarningRecord($"Viewer source open refused: {lRefusal}");
            return;
        }

        lViewer.LViewerRequestSet(lSourcePath);
        if (LLibrarian.LLibrarianFileCheck(lSourcePath))
        {
            if (LViewerSidecarResolve(lSourcePath) is not { } lResolvedPath)
            {
                LTraceLog.LTraceWarningRecord(
                    "Viewer source open refused: sidecar (.cad) source could not be resolved");
                return;
            }

            lSourcePath = lResolvedPath;
        }

        LPreference.LPreferenceMediaSet(lSourcePath);
        if (!LViewerCropPersistent)
        {
            lViewer.LViewerPreviewSet(lViewer.LViewerPreview.LRotateFlipChange(LRotateFlip.LRotateDefaultCreate()));
        }

        _ = lViewer.LViewerMedia.LViewerLoadStart(new LViewerIntent(lSourcePath, TimeSpan.Zero, null));
    }

    private string? LViewerSidecarResolve(string lSidecarPath)
    {
        LSidecarSourceResult? lResult = LLibrarian.LLibrarianSourceResolve(lSidecarPath);
        if (lResult is null)
        {
            LViewerAskRaise(LViewerAskCreate(
                LViewerAskKind.LViewerAskWarning,
                LLocalization.LLocalizationTextRead("Viewer.Sidecar.ReadError")));
            return null;
        }

        if (lResult.LSidecarResultVerified)
        {
            return lResult.LSidecarResultPath;
        }

        if (lResult.LSidecarResultKind != LSidecarSourceKind.LSidecarSourceMissing
            && LViewerAskRaise(LViewerAskCreate(
                LViewerAskKind.LViewerAskDecision,
                LLocalization.LLocalizationFormat("Viewer.Sidecar.MismatchFound", lResult.LSidecarResultPath))))
        {
            return lResult.LSidecarResultPath;
        }

        return LViewerSidecarFind(lSidecarPath, lResult.LSidecarResultName);
    }

    private string? LViewerSidecarFind(string lSidecarPath, string lSidecarName)
    {
        string? lChosen = null;
        LViewerSourceLocate?.Invoke(
            new LViewerAsk(
                LViewerAskKind.LViewerAskDecision,
                LLocalization.LLocalizationFormat("Viewer.Locate.Title", lSidecarName),
                string.Empty,
                string.Empty,
                string.Empty,
                lSidecarName,
                LLocalization.LLocalizationTextRead("Viewer.Dialog.MediaFilter")),
            lPath => lChosen = lPath);
        if (lChosen is null)
        {
            return null;
        }

        if (LLibrarian.LLibrarianSourceMatch(lChosen, lSidecarPath))
        {
            return lChosen;
        }

        return LViewerAskRaise(LViewerAskCreate(
            LViewerAskKind.LViewerAskDecision,
            LLocalization.LLocalizationTextRead("Viewer.Sidecar.MismatchSelected")))
            ? lChosen
            : null;
    }

    private static LViewerAsk LViewerAskCreate(LViewerAskKind lKind, string lMessage) =>
        new(
            lKind,
            LLocalization.LLocalizationTextRead("Viewer.Dialog.OpenTitle"),
            lMessage,
            LLocalization.LLocalizationTextRead("Terms.Open"),
            LLocalization.LLocalizationTextRead("Terms.Cancel"),
            string.Empty,
            string.Empty);

    private bool LViewerAskRaise(LViewerAsk lAsk)
    {
        bool lAnswer = false;
        LViewerSourceAsk?.Invoke(lAsk, lChoice => lAnswer = lChoice);
        return lAnswer;
    }

    public LWindowDropEffect LViewerDropResolve(string[]? lPaths, bool lCopyable)
    {
        IReadOnlyList<string> lList = lPaths ?? [];
        if (LViewerPathsDrop is not null)
        {
            return LWindowDrop.LWindowMediaCheck(lList) && lCopyable
                ? LWindowDropEffect.LWindowDropFile
                : LWindowDropEffect.LWindowDropNone;
        }

        string? lSourcePath = LWindowDrop.LWindowFileFind(lList);
        if (lSourcePath is null || (LMedia.LMediaAudioCheck(lSourcePath) && !lViewer.LViewerAudioAllowed))
        {
            return LWindowDropEffect.LWindowDropNone;
        }

        return lCopyable ? LWindowDropEffect.LWindowDropFile : LWindowDropEffect.LWindowDropNone;
    }

    public LWindowDropEffect LViewerDropHandle(string[]? lPaths, bool lCopyable)
    {
        LWindowDropEffect lEffect = LViewerDropResolve(lPaths, lCopyable);
        if (lEffect == LWindowDropEffect.LWindowDropNone)
        {
            return lEffect;
        }

        IReadOnlyList<string> lList = lPaths ?? [];
        if (LViewerPathsDrop is not null)
        {
            LViewerPathsDrop.Invoke(lList);
            return lEffect;
        }

        string? lSourcePath = LWindowDrop.LWindowFileFind(lList);
        if (lSourcePath is null)
        {
            return LWindowDropEffect.LWindowDropNone;
        }

        LViewerSourceOpen(lSourcePath);
        return lEffect;
    }

}
