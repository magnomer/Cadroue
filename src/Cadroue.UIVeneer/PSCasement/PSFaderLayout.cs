using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSFaderLayout
{
    internal static UIElement PSFaderLayoutBuild(
        Slider pSlider,
        IReadOnlyList<string> pLabels,
        int pIndex,
        TextBox pReadout,
        UIElement? pNotice = null)
    {
        int pLast = PSFaderLastResolve(pLabels);
        int pStart = PSFaderIndexClamp(pIndex, pLast);
        DoubleCollection pTicks = PSFaderTicksCreate(pLast);
        PSFaderLayoutApply(pSlider, pLast, pTicks, pStart);
        PSFader.PSFaderEditApply(pReadout, true);
        PSFader.PSFaderReadoutSet(pReadout, PSFaderLabelResolve(pLabels, pStart));
        PSFader.PSFaderNoticeSet(pNotice, PSFader.PSFaderNoticeResolve(pStart));
        PSFaderLayoutAttach(pSlider, pReadout, pLabels, pLast, pNotice);
        return PSFader.PSFaderRowBuild(pSlider, pReadout);
    }

    private static int PSFaderLastResolve(IReadOnlyList<string> pLabels) => Math.Max(pLabels.Count - 1, 0);

    private static int PSFaderIndexClamp(double pPosition, int pLast) =>
        Math.Clamp((int)Math.Round(pPosition), 0, pLast);

    private static DoubleCollection PSFaderTicksCreate(int pLast)
    {
        var pTicks = new DoubleCollection();
        for (int pTick = 0; pTick <= pLast; pTick++)
        {
            pTicks.Add(pTick);
        }

        return pTicks;
    }

    private static void PSFaderLayoutApply(Slider pSlider, int pLast, DoubleCollection pTicks, int pStart)
    {
        pSlider.Minimum = 0;
        pSlider.Maximum = pLast;
        pSlider.SmallChange = 1;
        pSlider.LargeChange = 1;
        pSlider.Ticks = pTicks;
        pSlider.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight;
        pSlider.IsSnapToTickEnabled = true;
        pSlider.Value = pStart;
        pSlider.Width = PSFader.PSFaderWidth;
        pSlider.VerticalAlignment = VerticalAlignment.Center;
        QSlider.QSliderAttach(pSlider);
        pSlider.IsSnapToTickEnabled = true;
    }

    private static string PSFaderLabelResolve(IReadOnlyList<string> pLabels, int pAt) =>
        pLabels.Count > 0 ? pLabels[pAt] : string.Empty;

    private static void PSFaderLayoutAttach(
        Slider pSlider,
        TextBox pReadout,
        IReadOnlyList<string> pLabels,
        int pLast,
        UIElement? pNotice) =>
        pSlider.ValueChanged += (_, _) => PSFaderLayoutHandle(pSlider, pReadout, pLabels, pLast, pNotice);

    private static void PSFaderLayoutHandle(
        Slider pSlider,
        TextBox pReadout,
        IReadOnlyList<string> pLabels,
        int pLast,
        UIElement? pNotice)
    {
        int pAt = PSFaderIndexClamp(pSlider.Value, pLast);
        PSFader.PSFaderReadoutSet(pReadout, PSFaderLabelResolve(pLabels, pAt));
        PSFader.PSFaderNoticeSet(pNotice, PSFader.PSFaderNoticeResolve(pAt));
    }
}
