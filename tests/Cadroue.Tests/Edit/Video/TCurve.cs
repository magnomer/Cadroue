using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCurve
{
    [Fact]
    public void Curve_NonIdentityMaster_CompilesToPchipExpression()
    {
        LWorkVideoStep step = TInterface.TWorkCurveCreate(
            true,
            master:
            [
                TInterface.TWorkPointCreate(0, 0),
                TInterface.TWorkPointCreate(0.5, 0.4),
                TInterface.TWorkPointCreate(1, 1)
            ]);

        Assert.Equal("curves=master='0/0 0.5/0.4 1/1':interp=pchip", TInterface.TWorkCurveFormat(step));
    }

    [Fact]
    public void Curve_IdentityChannels_CompileToEmptyString()
    {
        LWorkVideoStep step = TInterface.TWorkCurveCreate(true);

        Assert.Equal("", TInterface.TWorkCurveFormat(step));
    }

    [Fact]
    public void Curve_Create_ClampsSortsAndDropsDuplicateInputs()
    {
        LWorkVideoStep step = TInterface.TWorkCurveCreate(
            true,
            red:
            [
                TInterface.TWorkPointCreate(1.5, -0.2),
                TInterface.TWorkPointCreate(0, 0),
                TInterface.TWorkPointCreate(0.5, 0.5),
                TInterface.TWorkPointCreate(0.5, 0.9)
            ]);

        LWorkCurveSettings settings = TInterface.TWorkCurveRead(step);

        Assert.Collection(
            settings.LWorkCurveRed,
            point => Assert.Equal((0d, 0d), (point.LWorkCurveInput, point.LWorkCurveOutput)),
            point => Assert.Equal((0.5d, 0.5d), (point.LWorkCurveInput, point.LWorkCurveOutput)),
            point => Assert.Equal((1d, 0d), (point.LWorkCurveInput, point.LWorkCurveOutput)));
    }

    [Fact]
    public void Curve_MultipleChannels_JoinWithPchipInterp()
    {
        LWorkVideoStep step = TInterface.TWorkCurveCreate(
            true,
            red: [TInterface.TWorkPointCreate(0, 0), TInterface.TWorkPointCreate(1, 0.8)],
            blue: [TInterface.TWorkPointCreate(0, 0.1), TInterface.TWorkPointCreate(1, 1)]);

        Assert.Equal(
            "curves=red='0/0 1/0.8':blue='0/0.1 1/1':interp=pchip",
            TInterface.TWorkCurveFormat(step));
    }

    [Fact]
    public void Curve_ActiveIdentity_IsNotVideoActive()
    {
        LWorkVideoStep identity = TInterface.TWorkCurveCreate(true);
        LWorkVideoStep shaped = TInterface.TWorkCurveCreate(
            true,
            master: [TInterface.TWorkPointCreate(0, 0.2), TInterface.TWorkPointCreate(1, 1)]);

        Assert.False(TInterface.TWorkVideoCreate([identity]).LWorkVideoActive);
        Assert.True(TInterface.TWorkVideoCreate([shaped]).LWorkVideoActive);
    }
}
