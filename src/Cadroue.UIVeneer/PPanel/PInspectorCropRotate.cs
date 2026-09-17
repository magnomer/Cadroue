using System.Globalization;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    public LRotateFlip PInspectorRotateRead() => new(
        PInspectorKindRead(),
        pInspectorFlipHorizontal.IsChecked == true,
        pInspectorFlipVertical.IsChecked == true);

    public void PInspectorOrientationApply(LRotateFlip pInspectorOld)
    {
        var pInspectorSource = new LWorkCrop(
            (int)Math.Round(PInspectorNumberRead(pInspectorInsetLeft)),
            (int)Math.Round(PInspectorNumberRead(pInspectorInsetTop)),
            (int)Math.Round(PInspectorNumberRead(pInspectorInsetRight)),
            (int)Math.Round(PInspectorNumberRead(pInspectorInsetBottom)),
            PInspectorAngleResolve(pInspectorOld.LRotateKind),
            pInspectorOld.LRotateFlipHorizontal,
            pInspectorOld.LRotateFlipVertical);

        if (!pInspectorSource.LWorkEdgeActive)
        {
            return;
        }

        LRotateFlip pInspectorNew = PInspectorRotateRead();
        LWorkCrop pInspectorMapped = LCropbox.LCropboxOrientationResolve(
            pInspectorSource,
            PInspectorAngleResolve(pInspectorNew.LRotateKind),
            pInspectorNew.LRotateFlipHorizontal,
            pInspectorNew.LRotateFlipVertical);

        bool pCropSuppressPrevious = pInspectorCropSuppress;
        pInspectorCropSuppress = true;
        try
        {
            pInspectorInsetLeft.Text = pInspectorMapped.LWorkCropLeft.ToString(CultureInfo.InvariantCulture);
            pInspectorInsetTop.Text = pInspectorMapped.LWorkCropTop.ToString(CultureInfo.InvariantCulture);
            pInspectorInsetRight.Text = pInspectorMapped.LWorkCropRight.ToString(CultureInfo.InvariantCulture);
            pInspectorInsetBottom.Text = pInspectorMapped.LWorkCropBottom.ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            pInspectorCropSuppress = pCropSuppressPrevious;
        }

        PInspectorCropRaise();
    }

    private static int PInspectorAngleResolve(LRotateKind pInspectorKind) => pInspectorKind switch
    {
        LRotateKind.LRotate90 => 90,
        LRotateKind.LRotate180 => 180,
        LRotateKind.LRotate270 => 270,
        _ => 0
    };

    private void PInspectorRotateRaise()
    {
        if (pInspectorCropSuppress)
        {
            return;
        }

        PInspectorRotateChange?.Invoke(new LRotateFlip(
            PInspectorKindRead(),
            pInspectorFlipHorizontal.IsChecked == true,
            pInspectorFlipVertical.IsChecked == true));
    }

    private LRotateKind PInspectorKindRead() => pInspectorRotateCombo.SelectedIndex switch
    {
        1 => LRotateKind.LRotate90,
        2 => LRotateKind.LRotate180,
        3 => LRotateKind.LRotate270,
        _ => LRotateKind.LRotateNone
    };
}
