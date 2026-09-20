using Cadroue.Application;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TWhitebalanceWheel
{
    private const double TNeutralSize = 120;
    private const double TNeutralDot = 11;

    [Fact]
    public void Press_RightOfCentre_TurnsManualAndPlacesDotThere()
    {
        LWhitebalance whitebalance = TInterface.TWhitebalanceCreate();
        TInterface.TWhitebalanceCapableSet(whitebalance, true, true);

        TInterface.TWhitebalanceWheelHandle(whitebalance, true, TNeutralSize - 6, TNeutralSize / 2, TNeutralSize);
        LNeutralDot dot = TInterface.TWhitebalanceDotRead(whitebalance, TNeutralSize, TNeutralDot);

        Assert.True(TInterface.TWhitebalanceManualRead(whitebalance));
        Assert.True(dot.LNeutralDotPresent);
        Assert.Equal((TNeutralSize / 2) + (TNeutralSize * 0.45) - (TNeutralDot / 2), dot.LNeutralDotLeft, 1);
        Assert.Equal((TNeutralSize / 2) - (TNeutralDot / 2), dot.LNeutralDotTop, 1);
        Assert.True(TInterface.TWhitebalanceValueRead(whitebalance).LWorkWhitebalanceRed
            != TInterface.TWhitebalanceValueRead(whitebalance).LWorkWhitebalanceBlue);
    }

    [Fact]
    public void Press_Unpressed_OrIncapable_Ignored()
    {
        LWhitebalance whitebalance = TInterface.TWhitebalanceCreate();

        TInterface.TWhitebalanceWheelHandle(whitebalance, true, TNeutralSize - 6, TNeutralSize / 2, TNeutralSize);
        Assert.False(TInterface.TWhitebalanceManualRead(whitebalance));

        TInterface.TWhitebalanceCapableSet(whitebalance, true, true);
        TInterface.TWhitebalanceWheelHandle(whitebalance, false, TNeutralSize - 6, TNeutralSize / 2, TNeutralSize);
        Assert.False(TInterface.TWhitebalanceManualRead(whitebalance));
        Assert.False(TInterface.TWhitebalanceDotRead(whitebalance, TNeutralSize, TNeutralDot).LNeutralDotPresent);
    }

    [Fact]
    public void Press_OutsideDisc_ClampsToRim()
    {
        LWhitebalance whitebalance = TInterface.TWhitebalanceCreate();
        TInterface.TWhitebalanceCapableSet(whitebalance, true, true);

        TInterface.TWhitebalanceWheelHandle(whitebalance, true, TNeutralSize * 3, TNeutralSize / 2, TNeutralSize);
        LNeutralDot dot = TInterface.TWhitebalanceDotRead(whitebalance, TNeutralSize, TNeutralDot);

        Assert.Equal((TNeutralSize / 2) + (TNeutralSize * 0.45) - (TNeutralDot / 2), dot.LNeutralDotLeft, 1);
    }

    [Fact]
    public void Bitmap_CoversSquare_TransparentOutsideDisc()
    {
        LNeutralBitmap bitmap = TInterface.TNeutralBitmapResolve(20, 0.9);

        Assert.Equal(20, bitmap.LNeutralBitmapSize);
        Assert.Equal(80, bitmap.LNeutralBitmapStride);
        Assert.Equal(20 * 80, bitmap.LNeutralBitmapPixels.Length);
        Assert.Equal(0, bitmap.LNeutralBitmapPixels[3]);
        Assert.Equal(255, bitmap.LNeutralBitmapPixels[(10 * 80) + (10 * 4) + 3]);
    }
}
