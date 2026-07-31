using System.IO;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private static string PRosterPendingFormat(string pMediaPath) =>
        pRosterMediaPending.Contains(pMediaPath) ? LLocalization.LLocalizationTextRead("Roster.Value.Reading") : LLocalization.LLocalizationTextRead("Roster.Value.Unknown");

    private static string PRosterMediaFormat(LWorkMedia? pMediaInfo, string pMediaPath)
    {
        if (pMediaInfo is null || !pMediaInfo.LWorkMediaVideoPresent)
        {
            return pMediaInfo is null ? PRosterPendingFormat(pMediaPath) : LLocalization.LLocalizationTextRead("Roster.AudioOnly");
        }

        return $"{pMediaInfo.LWorkMediaWidth} x {pMediaInfo.LWorkMediaHeight}  /  " +
            $"{pMediaInfo.LWorkMediaFrameRate:0.###} fps";
    }

    private static string PRosterContainerFormat(string pMediaPath)
    {
        string pExtension = Path.GetExtension(pMediaPath).TrimStart('.');
        return pExtension.Length == 0 ? LLocalization.LLocalizationTextRead("Roster.Value.Unknown") : pExtension.ToUpperInvariant();
    }

    private static string PRosterFlipFormat(LWorkCrop pCrop)
    {
        if (pCrop.LWorkCropFlipHorizontal && pCrop.LWorkCropFlipVertical)
        {
            return LLocalization.LLocalizationTextRead("Roster.Value.HorizontalVertical");
        }

        return pCrop.LWorkCropFlipHorizontal ? LLocalization.LLocalizationTextRead("Inspector.Crop.Horizontal") : LLocalization.LLocalizationTextRead("Inspector.Crop.Vertical");
    }

    private static string PRosterRatioFormat(LWorkItem pWorkItem, LWorkMedia? pSourceInfo)
    {
        if (PRosterCropRead(pWorkItem, pSourceInfo) is not { } pCropSize)
        {
            return LLocalization.LLocalizationTextRead("Roster.Value.Unknown");
        }

        int pDivisor = PRosterDivisorRead(pCropSize.PRosterWidth, pCropSize.PRosterHeight);
        return $"{pCropSize.PRosterWidth / pDivisor} : {pCropSize.PRosterHeight / pDivisor}";
    }

    private static string PRosterResolutionFormat(LWorkItem pWorkItem, LWorkMedia? pSourceInfo)
    {
        if (PRosterCropRead(pWorkItem, pSourceInfo) is not { } pCropSize)
        {
            return LLocalization.LLocalizationTextRead("Roster.Value.Unknown");
        }

        int pWidth = pCropSize.PRosterWidth;
        int pHeight = pCropSize.PRosterHeight;

        string[] pSizeParts = pWorkItem.LWorkOutput.LWorkOutputVideoSize.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pSizeParts.Length == 2
            && int.TryParse(pSizeParts[0], out int pSizeWidth)
            && int.TryParse(pSizeParts[1], out int pSizeHeight))
        {
            bool pPortrait = pHeight > pWidth;
            int pShortEdge = Math.Min(pSizeWidth, pSizeHeight);
            int pLongEdge = Math.Max(pSizeWidth, pSizeHeight);
            pWidth = pWorkItem.LWorkOutput.LWorkSizeReactive && pPortrait ? pShortEdge : pSizeWidth;
            pHeight = pWorkItem.LWorkOutput.LWorkSizeReactive && pPortrait ? pLongEdge : pSizeHeight;
        }

        return $"{pWidth} x {pHeight}";
    }

    private static (int PRosterWidth, int PRosterHeight)? PRosterCropRead(LWorkItem pWorkItem, LWorkMedia? pSourceInfo)
    {
        if (pSourceInfo is null || !pSourceInfo.LWorkMediaVideoPresent)
        {
            return null;
        }

        LWorkCrop pCrop = pWorkItem.LWorkCrop;
        int pWidth = pSourceInfo.LWorkMediaWidth;
        int pHeight = pSourceInfo.LWorkMediaHeight;
        if (pCrop.LWorkCropRotation is 90 or 270)
        {
            (pWidth, pHeight) = (pHeight, pWidth);
        }

        pWidth -= pCrop.LWorkCropLeft + pCrop.LWorkCropRight;
        pHeight -= pCrop.LWorkCropTop + pCrop.LWorkCropBottom;
        return pWidth > 0 && pHeight > 0 ? (pWidth, pHeight) : null;
    }

    private static int PRosterDivisorRead(int pFirst, int pSecond)
    {
        while (pSecond != 0)
        {
            (pFirst, pSecond) = (pSecond, pFirst % pSecond);
        }

        return pFirst == 0 ? 1 : pFirst;
    }

    private static string PRosterPhaseFormat(LWorkState pWorkState, LWorkPhase pWorkPhase) => pWorkState switch
    {
        LWorkState.LWorkStateDone => LLocalization.LLocalizationTextRead("Roster.State.Done"),
        LWorkState.LWorkStateFailed => LLocalization.LLocalizationTextRead("Roster.State.Failed"),
        _ => pWorkPhase switch
        {
            LWorkPhase.LWorkPhaseEncoding => LLocalization.LLocalizationTextRead("Roster.Phase.Processing"),
            LWorkPhase.LWorkPhaseStarted => LLocalization.LLocalizationTextRead("Roster.Phase.Started"),
            _ => LLocalization.LLocalizationTextRead("Roster.Phase.NotStarted")
        }
    };

    private static string PRosterKindFormat(LWorkKind pWorkKind) => pWorkKind switch
    {
        LWorkKind.LWorkKindSplit => LLocalization.LLocalizationTextRead("Roster.Kind.Split"),
        LWorkKind.LWorkKindEdit => LLocalization.LLocalizationTextRead("Roster.Kind.Edit"),
        LWorkKind.LWorkKindAudio => LLocalization.LLocalizationTextRead("Roster.Kind.Audio"),
        LWorkKind.LWorkKindConvert => LLocalization.LLocalizationTextRead("Roster.Kind.Convert"),
        LWorkKind.LWorkKindMerge => LLocalization.LLocalizationTextRead("Roster.Kind.Merge"),
        _ => pWorkKind.ToString()
    };
}
