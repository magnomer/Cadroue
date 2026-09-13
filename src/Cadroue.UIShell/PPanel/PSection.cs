using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PFlow;
using Cadroue.UIShell.PDeck;
using Cadroue.UIShell.PHouse;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PSection : UserControl
{
    private static readonly FontFamily pSectionFontFamily = new("Segoe UI");

    internal const double PSectionNameSize = 12;

    private const double PSectionActionGap = 16;
    private const double PSectionBadgeSize = 18;
    private const double PSectionBadgePadding = 6;
    private const double PSectionDisabledOpacity = 0.4;

    private PFlowControl? pFlowAttached;
    private IReadOnlyList<LPiece> pSectionListCurrent = Array.Empty<LPiece>();
    private HashSet<int> pSectionSelectedCurrent = new();
    private readonly TextBlock pSectionCountLabel;
    private readonly StackPanel pSectionRowPanel;
    private bool pSectionRebuilding;

    public PSection()
    {
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
        UIElement pActionBar = PSectionActionBuild();
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
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

    public void PSectionAttach(PFlowControl pFlow)
    {
        PSectionDetach();
        pFlowAttached = pFlow;
        pFlowAttached.PFlowSectionChange += PSectionUpdateHandle;
        PSectionRebuild();
    }

    private void PSectionDetach()
    {
        if (pFlowAttached is null) return;
        pFlowAttached.PFlowSectionChange -= PSectionUpdateHandle;
        pFlowAttached = null;
    }

    private void PSectionUpdateHandle(IReadOnlyList<LPiece> pSectionList, int? pSectionIndexSelect)
    {
        LPiece[] pSectionListNext = pSectionList.ToArray();
        bool pSectionListSame = pSectionListCurrent.SequenceEqual(pSectionListNext)
            && pSectionRowPanel.Children.Count == pSectionListNext.Length;
        pSectionListCurrent = pSectionListNext;
        pSectionSelectedCurrent = pFlowAttached is null
            ? new HashSet<int>()
            : new HashSet<int>(pFlowAttached.PFlowSelectedRead());

        if (pSectionDragActive)
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
        pSectionRebuilding = true;
        try
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
        finally
        {
            pSectionRebuilding = false;
        }
    }
}
