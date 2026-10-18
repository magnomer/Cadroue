using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PProcessingStack
{
    private static readonly Thickness pProcessingStepPadding = new(12, 5, 12, 5);
    private static readonly Thickness pProcessingStepLine = new(0, 0, 0, 1);
    private static readonly Thickness pProcessingSkipPadding = new(12, 5, 12, 5);
    private static readonly Thickness pProcessingSkipLine = new(0, 1, 0, 0);

    private static readonly IReadOnlyDictionary<Key, bool> pProcessingActivateKeys = new Dictionary<Key, bool>
    {
        [Key.Enter] = true,
        [Key.Space] = true,
    };

    private const string PProcessingSkipIcon = "/PAsset/PPanel/PProcessingSkip.svg";

    private readonly QProcessing lProcessing;
    private readonly PProcessingRow pProcessingSkipRow;
    private readonly Dictionary<string, PProcessingRow> pProcessingRows = new(StringComparer.Ordinal);

    public PProcessingStack(QProcessing lProcessingOwner)
    {
        lProcessing = lProcessingOwner;
        pProcessingSkipRow = PProcessingSkipBuild();
        PProcessingPanelAttach();
    }

    public StackPanel PProcessingRowPanel { get; } = new();

    public Border PProcessingSkipBorder => pProcessingSkipRow.PProcessingRowBorder;

    private void PProcessingPanelAttach()
    {
        PProcessingRowPanel.PreviewMouseMove += PProcessingMoveHandle;
        PProcessingRowPanel.MouseLeftButtonUp += PProcessingUpHandle;
        PProcessingRowPanel.LostMouseCapture += PProcessingLostHandle;
        pProcessingSkipRow.PProcessingRowBorder.MouseLeftButtonUp += (_, _) =>
            lProcessing.QProcessingStepSelect(QProcessing.QProcessingSkipStep);
    }

    public void PProcessingRowAdd(QProcessingRow lRow)
    {
        PProcessingRow pRow = PProcessingStepBuild(lRow);
        pProcessingRows.Add(lRow.QProcessingRowKey, pRow);
        PProcessingRowPanel.Children.Add(pRow.PProcessingRowBorder);
        lProcessing.QProcessingRowAdd(lRow);
    }

    private PProcessingRow PProcessingStepBuild(QProcessingRow lRow)
    {
        PProcessingRow pRow = PProcessingRowBuild(lRow);
        PProcessingStepAttach(lRow.QProcessingRowKey, pRow.PProcessingRowBorder);
        return pRow;
    }

    private static PProcessingRow PProcessingRowBuild(QProcessingRow lRow)
    {
        var pRow = new PProcessingRow(
            lRow.QProcessingRowIcon,
            QLocalizationCatalog.QLocalizationTextRead(lRow.QProcessingRowLabel),
            pProcessingStepPadding,
            pProcessingStepLine);
        Border pBorder = pRow.PProcessingRowBorder;
        pBorder.Focusable = true;
        ToolTipService.SetShowOnDisabled(pBorder, true);
        return pRow;
    }

    private void PProcessingStepAttach(string pStepName, Border pBorder)
    {
        pBorder.MouseLeftButtonDown += (_, pEvent) => PProcessingPressHandle(pStepName, pEvent);
        pBorder.KeyDown += (_, pEvent) => PProcessingKeyHandle(pStepName, pEvent);
    }

    private static PProcessingRow PProcessingSkipBuild()
    {
        var pRow = new PProcessingRow(
            PProcessingSkipIcon,
            QLocalizationCatalog.QLocalizationTextRead("Processing.Skip.Label"),
            pProcessingSkipPadding,
            pProcessingSkipLine);
        pRow.PProcessingRowBorder.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Processing.Skip.Tooltip");
        return pRow;
    }

    private void PProcessingPressHandle(string pStepName, MouseButtonEventArgs pEvent)
    {
        Point pPoint = pEvent.GetPosition(PProcessingRowPanel);
        lProcessing.QProcessingDrag.QProcessingDragStart(pStepName, pPoint.X, pPoint.Y);
        pEvent.Handled = true;
    }

    private void PProcessingKeyHandle(string pStepName, KeyEventArgs pEvent)
    {
        bool pActivate = pProcessingActivateKeys.GetValueOrDefault(pEvent.Key);
        lProcessing.QProcessingKeyHandle(pStepName, pActivate);
        pEvent.Handled = pActivate;
    }

    private void PProcessingMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        Point pPoint = pEvent.GetPosition(PProcessingRowPanel);
        lProcessing.QProcessingDrag.QProcessingDragMove(
            pPoint.X,
            pPoint.Y,
            PLook.PLookPressed[pEvent.LeftButton],
            SystemParameters.MinimumHorizontalDragDistance,
            SystemParameters.MinimumVerticalDragDistance,
            lProcessing.QProcessingSteps.Select(PProcessingTopRead).ToList(),
            lProcessing.QProcessingSteps.Select(PProcessingHeightRead).ToList());
    }

    private void PProcessingUpHandle(object pSender, MouseButtonEventArgs pEvent) =>
        lProcessing.QProcessingDrag.QProcessingDragClear();

    private void PProcessingLostHandle(object pSender, MouseEventArgs pEvent) =>
        lProcessing.QProcessingDrag.QProcessingDragClear();

    public static void PProcessingCaptureClear() => Mouse.Capture(null);

    private double PProcessingTopRead(string pStepName) =>
        pProcessingRows[pStepName].PProcessingRowBorder.TranslatePoint(new Point(0, 0), PProcessingRowPanel).Y;

    private double PProcessingHeightRead(string pStepName) =>
        pProcessingRows[pStepName].PProcessingRowBorder.ActualHeight;

    private Border PProcessingBorderRead(string pStepName) => pProcessingRows[pStepName].PProcessingRowBorder;

    public void PProcessingPanelRebuild()
    {
        PProcessingRowPanel.Children.Clear();
        lProcessing.QProcessingSteps
            .Select(PProcessingBorderRead)
            .ToList()
            .ForEach(pBorder => PProcessingRowPanel.Children.Add(pBorder));
    }

    public void PProcessingStackUpdate()
    {
        lProcessing.QProcessingSteps.ToList().ForEach(PProcessingRowUpdate);
        pProcessingSkipRow.PProcessingSelectApply(lProcessing.QProcessingSkipSelected);
        pProcessingSkipRow.PProcessingActiveApply(lProcessing.QProcessingSkipActive);
        PProcessingRowPanel.Opacity = lProcessing.QProcessingSkipOpacity;
    }

    private void PProcessingRowUpdate(string pStepName)
    {
        PProcessingRow pRow = pProcessingRows[pStepName];
        pRow.PProcessingStateApply(
            lProcessing.QProcessingEnabledCheck(pStepName),
            lProcessing.QProcessingOpacityRead(pStepName),
            lProcessing.QProcessingNoticeRead(pStepName),
            lProcessing.QProcessingNoticeFormat(pStepName));
        pRow.PProcessingSelectApply(lProcessing.QProcessingSelectedCheck(pStepName));
        pRow.PProcessingActiveApply(lProcessing.QProcessingActiveCheck(pStepName));
        pRow.PProcessingNumberApply(lProcessing.QProcessingNumberRead(pStepName), lProcessing.QProcessingOrdered);
    }
}
