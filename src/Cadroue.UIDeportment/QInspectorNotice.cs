using Cadroue.Application;

namespace Cadroue.UIDeportment;

internal static class QInspectorNotice
{
    internal static string? QInspectorNoticeResolve(
        bool lSourcePresent, double lCropWidth, double lCropHeight, LCropboxRatio lRatio)
    {
        if (!lSourcePresent)
        {
            return null;
        }

        if (lCropWidth <= 0 || lCropHeight <= 0)
        {
            return QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.FrameError");
        }

        if (!lRatio.LCropboxRatioFixed)
        {
            return null;
        }

        if (lRatio.LCropboxRatioWidth <= 0 || lRatio.LCropboxRatioHeight <= 0)
        {
            return QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.RatioError");
        }

        if (lRatio.LCropboxRatioLenient
            && LCropbox.LCropboxToleranceCheck(
                lCropWidth, lCropHeight, lRatio.LCropboxRatioWidth, lRatio.LCropboxRatioHeight))
        {
            return null;
        }

        (int lExcess, bool lWide) = LCropbox.LCropboxExcessResolve(
            lCropWidth, lCropHeight, lRatio.LCropboxRatioWidth, lRatio.LCropboxRatioHeight);
        if (lExcess <= 0)
        {
            return null;
        }

        return QLocalizationCatalog.QLocalizationFormat(
            lWide ? "Inspector.Crop.WidthMismatch" : "Inspector.Crop.HeightMismatch", lExcess);
    }
}
