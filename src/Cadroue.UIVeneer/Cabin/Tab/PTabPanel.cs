using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PTabPanel
{
    private const double PTabMinimumDefault = 180;

    internal sealed record PTabCollapse(
        Func<bool> PTabReader,
        Action<bool> PTabWriter,
        Action<Action<bool>> PTabListener);

    private static readonly IReadOnlyDictionary<Type, double> pTabMinimums = new Dictionary<Type, double>
    {
        [typeof(PExport)] = 300,
        [typeof(PViewer)] = 320,
        [typeof(PSection)] = 300,
        [typeof(PGroup)] = 260,
        [typeof(PList)] = 300,
        [typeof(PProcessing)] = 184,
        [typeof(PInspector)] = 360,
        [typeof(PClinic)] = 300,
    };

    private static readonly IReadOnlyDictionary<Type, bool> pTabCompacts = new Dictionary<Type, bool>
    {
        [typeof(PList)] = true,
        [typeof(PExport)] = true,
        [typeof(PProcessing)] = true,
        [typeof(PInspector)] = true,
        [typeof(PClinic)] = true,
        [typeof(PSection)] = true,
    };

    private static readonly IReadOnlyDictionary<Type, double> pTabStrips = new Dictionary<Type, double>
    {
        [typeof(PList)] = PList.PListStripWidth,
        [typeof(PProcessing)] = PProcessing.PProcessingStripWidth,
        [typeof(PInspector)] = PInspector.PInspectorStripWidth,
        [typeof(PClinic)] = PClinic.PClinicStripWidth,
        [typeof(PSection)] = PSection.PSectionStripWidth,
    };

    private static readonly IReadOnlyDictionary<Type, Func<UIElement, PTabCollapse>> pTabCollapses =
        new Dictionary<Type, Func<UIElement, PTabCollapse>>
        {
            [typeof(PList)] = pPanel => PTabCollapseCreate((PList)pPanel),
            [typeof(PProcessing)] = pPanel => PTabCollapseCreate((PProcessing)pPanel),
            [typeof(PInspector)] = pPanel => PTabCollapseCreate((PInspector)pPanel),
            [typeof(PClinic)] = pPanel => PTabCollapseCreate((PClinic)pPanel),
            [typeof(PSection)] = pPanel => PTabCollapseCreate((PSection)pPanel),
        };

    public static PTabCollapse? PTabCollapseRead(UIElement pPanel) =>
        pTabCollapses.GetValueOrDefault(pPanel.GetType())?.Invoke(pPanel);

    public static double PTabStripRead(UIElement pPanel) => pTabStrips.GetValueOrDefault(pPanel.GetType(), 0);

    public static bool PTabCompactRead(UIElement pPanel) => pTabCompacts.GetValueOrDefault(pPanel.GetType(), false);

    public static ColumnDefinition PTabColumnBuild(UIElement pPanel) => new()
    {
        Width = new GridLength(1, GridUnitType.Star),
        MinWidth = QSurfaceGrid.QSurfaceMinimumResolve(
            ((FrameworkElement)pPanel).MinWidth,
            pTabMinimums.GetValueOrDefault(pPanel.GetType(), PTabMinimumDefault))
    };

    private static PTabCollapse PTabCollapseCreate(PList pPanel) =>
        new(
            pPanel.PListMinimizedCheck,
            pPanel.PListMinimizeSet,
            pHandler => pPanel.QList.QListMinimizeChanged += pHandler);

    private static PTabCollapse PTabCollapseCreate(PProcessing pPanel) =>
        new(
            pPanel.PProcessingMinimizedCheck,
            pPanel.PProcessingMinimizeSet,
            pHandler => pPanel.PProcessingMinimizeChanged += pHandler);

    private static PTabCollapse PTabCollapseCreate(PInspector pPanel) =>
        new(
            pPanel.PInspectorMinimizedCheck,
            pPanel.PInspectorMinimizeSet,
            pHandler => pPanel.QInspector.QInspectorMinimizeChanged += pHandler);

    private static PTabCollapse PTabCollapseCreate(PClinic pPanel) =>
        new(
            pPanel.PClinicMinimizedCheck,
            pPanel.PClinicMinimizeSet,
            pHandler => pPanel.PClinicMinimizeChanged += pHandler);

    private static PTabCollapse PTabCollapseCreate(PSection pPanel) =>
        new(
            pPanel.PSectionMinimizedCheck,
            pPanel.PSectionMinimizeSet,
            pHandler => pPanel.QSection.QSectionMinimizeChanged += pHandler);
}
