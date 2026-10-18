using Cadroue.Core;

namespace Cadroue.Application;

public static class LCropboxOrientation
{
    public static bool LCropboxRotatedCheck(int lCropboxRotation) =>
        ((lCropboxRotation / 90 % 4 + 4) % 4) % 2 != 0;

    public static LWorkCrop LCropboxOrientationResolve(
        LWorkCrop lCropbox,
        int lCropboxRotation,
        bool lCropboxFlipHorizontal,
        bool lCropboxFlipVertical)
    {
        int[] lCropboxOld = LCropboxPermutationResolve(
            lCropbox.LWorkCropRotation, lCropbox.LWorkFlipHorizontal, lCropbox.LWorkFlipVertical);
        int[] lCropboxNew = LCropboxPermutationResolve(
            lCropboxRotation, lCropboxFlipHorizontal, lCropboxFlipVertical);

        int[] lCropboxEdges =
        {
            lCropbox.LWorkCropLeft, lCropbox.LWorkCropTop, lCropbox.LWorkCropRight, lCropbox.LWorkCropBottom
        };
        int[] lCropboxResult = new int[4];
        for (int lCropboxLabel = 0; lCropboxLabel < 4; lCropboxLabel++)
        {
            lCropboxResult[lCropboxNew[lCropboxLabel]] = lCropboxEdges[lCropboxOld[lCropboxLabel]];
        }

        return lCropbox with
        {
            LWorkCropLeft = lCropboxResult[0],
            LWorkCropTop = lCropboxResult[1],
            LWorkCropRight = lCropboxResult[2],
            LWorkCropBottom = lCropboxResult[3],
            LWorkCropRotation = lCropboxRotation,
            LWorkFlipHorizontal = lCropboxFlipHorizontal,
            LWorkFlipVertical = lCropboxFlipVertical
        };
    }

    private static int[] LCropboxPermutationResolve(
        int lCropboxRotation,
        bool lCropboxFlipHorizontal,
        bool lCropboxFlipVertical)
    {
        int lCropboxSteps = ((lCropboxRotation / 90) % 4 + 4) % 4;
        int[] lCropboxMap = new int[4];
        for (int lCropboxLabel = 0; lCropboxLabel < 4; lCropboxLabel++)
        {
            int lCropboxPosition = lCropboxLabel;
            if (lCropboxFlipHorizontal)
            {
                lCropboxPosition = lCropboxPosition == 0 ? 2 : lCropboxPosition == 2 ? 0 : lCropboxPosition;
            }

            if (lCropboxFlipVertical)
            {
                lCropboxPosition = lCropboxPosition == 1 ? 3 : lCropboxPosition == 3 ? 1 : lCropboxPosition;
            }

            for (int lCropboxTurn = 0; lCropboxTurn < lCropboxSteps; lCropboxTurn++)
            {
                lCropboxPosition = (lCropboxPosition + 1) % 4;
            }

            lCropboxMap[lCropboxLabel] = lCropboxPosition;
        }

        return lCropboxMap;
    }

    public static LCropboxSize LCropboxSourceResolve(
        double lCropboxSourceWidth,
        double lCropboxSourceHeight,
        bool lCropboxRotated) =>
        lCropboxRotated
            ? new LCropboxSize(lCropboxSourceHeight, lCropboxSourceWidth)
            : new LCropboxSize(lCropboxSourceWidth, lCropboxSourceHeight);
}
