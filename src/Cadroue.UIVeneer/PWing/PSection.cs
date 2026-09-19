using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PCabin;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PSection : UserControl
{
    private static readonly FontFamily pSectionFontFamily = new("Segoe UI");

    internal const double PSectionNameSize = 12;

    private const double PSectionActionGap = 16;
    private const double PSectionBadgeSize = 18;
    private const double PSectionBadgePadding = 6;
    private const double PSectionDisabledOpacity = 0.4;

    private PFlow? pFlowAttached;
    private IReadOnlyList<LPiece> pSectionListCurrent = Array.Empty<LPiece>();
    private HashSet<int> pSectionSelectedCurrent = new();
    private readonly TextBlock pSectionCountLabel;
    private readonly StackPanel pSectionRowPanel;
    private readonly UIElement pSectionActionBar;

    public LSection LSection { get; } = new();

    public PSection()
    {
        LSection.LSectionMinimizeChange += PSectionMinimizeHandle;
        pSectionCountLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Section.Header.Title"),
            FontSize = 12,
            FontFamily = pSectionFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        UIElement pHeader = PSectionHeaderBuild();

        pSectionRowPanel = new StackPanel();
        pSectionRowPanel.PreviewMouseMove += PSectionMoveHandle;
        pSectionRowPanel.MouseLeftButtonUp += PSectionUpHandle;
        pSectionRowPanel.LostMouseCapture += PSectionLostHandle;

        var pScroll = new ScrollViewer
        {
            Content = pSectionRowPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pSectionActionBar = PSectionActionBuild();
        DockPanel.SetDock(pSectionActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pSectionActionBar);
        pRoot.Children.Add(pScroll);

        pSectionFullBody = pRoot;
        pSectionStripBody = PSectionStripBuild();
        pSectionStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pSectionFullBody);
        pBodyHost.Children.Add(pSectionStripBody);

        FocusVisualStyle = null;
        PScrollbar.PScrollbarApply(this);
        Content = PPanel.PPanelBorderBuild(pBodyHost);
    }

    public void PSectionAttach(PFlow pFlow)
    {
        PSectionDetach();
        pFlowAttached = pFlow;
        pFlowAttached.LFlow.LFlowSection.LFlowSectionChange += PSectionUpdateHandle;
        pFlowAttached.LFlow.LFlowEditChange += PSectionEditHandle;
        PSectionEditHandle(pFlow.LFlow.LFlowSectionEditable);
        PSectionRebuild();
    }

    private void PSectionDetach()
    {
        if (pFlowAttached is null) return;
        pFlowAttached.LFlow.LFlowSection.LFlowSectionChange -= PSectionUpdateHandle;
        pFlowAttached.LFlow.LFlowEditChange -= PSectionEditHandle;
        pFlowAttached = null;
    }

    private void PSectionEditHandle(bool pSectionEdit)
    {
        LSection.LSectionEditableSet(pSectionEdit);
        pSectionActionBar.IsEnabled = pSectionEdit;
        if (pSectionEdit) return;
        pSectionRowPanel.ReleaseMouseCapture();
        PSectionDragClear();
        if (LSection.LSectionEditIndex is not null) PSectionEditCancel();
    }

    private void PSectionUpdateHandle(IReadOnlyList<LPiece> pSectionList, int? pSectionIndexSelect)
    {
        LPiece[] pSectionListNext = pSectionList.ToArray();
        bool pSectionListSame = pSectionListCurrent.SequenceEqual(pSectionListNext)
            && pSectionRowPanel.Children.Count == pSectionListNext.Length;
        pSectionListCurrent = pSectionListNext;
        pSectionSelectedCurrent = pFlowAttached is null
            ? new HashSet<int>()
            : new HashSet<int>(pFlowAttached.LFlow.LFlowSection.LFlowSelectedRead());

        if (LSection.LSectionDragActive)
        {
            return;
        }

        if (pSectionListSame)
        {
            PSectionSelectApply();
            return;
        }

        PSectionRebuild();
    }

    private void PSectionSelectApply()
    {
        for (int pIndex = 0; pIndex < pSectionRowPanel.Children.Count; pIndex++)
        {
            if (pSectionRowPanel.Children[pIndex] is Border pRow)
            {
                pRow.Background = pSectionSelectedCurrent.Contains(pIndex)
                    ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB))
                    : Brushes.White;
            }
        }
    }

    private void PSectionRebuild()
    {
        pSectionRowPanel.Children.Clear();
        int pCount = pSectionListCurrent.Count;
        pSectionCountLabel.Text = pCount == 0
            ? LLocalization.LLocalizationTextRead("Section.Header.Title")
            : LLocalization.LLocalizationFormat("Section.Header.Count", pCount);
        for (int i = 0; i < pCount; i++)
        {
            pSectionRowPanel.Children.Add(
                PSectionRowBuild(i, pSectionListCurrent[i], pSectionSelectedCurrent.Contains(i)));
        }
    }
}
