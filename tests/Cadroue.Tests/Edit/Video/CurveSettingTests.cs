using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class CurveSettingTests
{
    [Fact]
    public void Curve_NonIdentityMaster_CompilesToPchipExpression()
    {
        LWorkVideoStep step = TInterface.WorkCurveCreate(
            true,
            master:
            [
                TInterface.WorkCurvePointCreate(0, 0),
                TInterface.WorkCurvePointCreate(0.5, 0.4),
                TInterface.WorkCurvePointCreate(1, 1)
            ]);

        Assert.Equal("curves=master='0/0 0.5/0.4 1/1':interp=pchip", TInterface.WorkCurveFormat(step));
    }

    [Fact]
    public void Curve_IdentityChannels_CompileToEmptyString()
    {
        LWorkVideoStep step = TInterface.WorkCurveCreate(true);

        Assert.Equal("", TInterface.WorkCurveFormat(step));
    }

    [Fact]
    public void Curve_Create_ClampsSortsAndDropsDuplicateInputs()
    {
        LWorkVideoStep step = TInterface.WorkCurveCreate(
            true,
            red:
            [
                TInterface.WorkCurvePointCreate(1.5, -0.2),
                TInterface.WorkCurvePointCreate(0, 0),
                TInterface.WorkCurvePointCreate(0.5, 0.5),
                TInterface.WorkCurvePointCreate(0.5, 0.9)
            ]);

        LWorkCurveSettings settings = TInterface.WorkCurveRead(step);

        Assert.Collection(
            settings.LWorkCurveRed,
            point => Assert.Equal((0d, 0d), (point.LWorkCurveInput, point.LWorkCurveOutput)),
            point => Assert.Equal((0.5d, 0.5d), (point.LWorkCurveInput, point.LWorkCurveOutput)),
            point => Assert.Equal((1d, 0d), (point.LWorkCurveInput, point.LWorkCurveOutput)));
    }

    [Fact]
    public void Curve_MultipleChannels_JoinWithPchipInterp()
    {
        LWorkVideoStep step = TInterface.WorkCurveCreate(
            true,
            red: [TInterface.WorkCurvePointCreate(0, 0), TInterface.WorkCurvePointCreate(1, 0.8)],
            blue: [TInterface.WorkCurvePointCreate(0, 0.1), TInterface.WorkCurvePointCreate(1, 1)]);

        Assert.Equal(
            "curves=red='0/0 1/0.8':blue='0/0.1 1/1':interp=pchip",
            TInterface.WorkCurveFormat(step));
    }

    [Fact]
    public void Curve_ActiveIdentity_IsNotVideoActive()
    {
        LWorkVideoStep identity = TInterface.WorkCurveCreate(true);
        LWorkVideoStep shaped = TInterface.WorkCurveCreate(
            true,
            master: [TInterface.WorkCurvePointCreate(0, 0.2), TInterface.WorkCurvePointCreate(1, 1)]);

        Assert.False(TInterface.WorkVideoCreate([identity]).LWorkVideoActive);
        Assert.True(TInterface.WorkVideoCreate([shaped]).LWorkVideoActive);
    }
}
