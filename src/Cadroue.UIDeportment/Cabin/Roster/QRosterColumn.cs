using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The roster's columns: the plan its surface lays them out by, the weights a saved layout keeps, and the panel width
// they need.
public sealed class QRosterColumn
{
    private const double QRosterWidthPadding = 16;

    private QColumnPlan? qRosterPlan;

    public QColumnPlan QRosterPlanCreate(IReadOnlyList<double> lMinimumWidths, IReadOnlyList<double>? lStoredWidths)
    {
        qRosterPlan = new QColumnPlan(lMinimumWidths, lStoredWidths, null, -1);
        return qRosterPlan;
    }

    public LSceneTabEntry QRosterLayoutRead()
    {
        var lLayout = new LSceneTabEntry();
        lLayout.LScenePanelWidths.AddRange(qRosterPlan?.QColumnWeightsRead() ?? []);
        return lLayout;
    }

    public static double QRosterWidthResolve(double lColumnTotal) => lColumnTotal + QRosterWidthPadding;
}
