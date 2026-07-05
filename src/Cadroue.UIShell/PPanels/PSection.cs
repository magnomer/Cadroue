using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PFlow;
using Cadroue.UIShell.PMainArea;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PPanels;

public sealed class PSection : UserControl
{
    private static readonly Color[] pSectionPaletteColors =
    {
        Color.FromRgb(0x4A, 0x90, 0xD9),
        Color.FromRgb(0x27, 0xAE, 0x60),
        Color.FromRgb(0xE6, 0x7E, 0x22),
        Color.FromRgb(0x8E, 0x44, 0xAD),
        Color.FromRgb(0xE7, 0x4C, 0x3C),
        Color.FromRgb(0x16, 0xA0, 0x85),
    };

    private static readonly FontFamily pSectionFontFamily = new("Segoe UI");

    private PFlowControl? pFlowAttached;
    private IReadOnlyList<LSegment> pSectionListCurrent = Array.Empty<LSegment>();
    private int? pSectionIndexSelectCurrent;
    private readonly TextBlock pSectionCountLabel;
    private readonly StackPanel pSectionRowPanel;

    public PSection()
    {
        pSectionCountLabel = new TextBlock
        {
            Text = "Sections",
            FontSize = 12,
            FontFamily = pSectionFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        var pHeader = new Border
        {
            Padding = new Thickness(12, 10, 12, 10),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pSectionCountLabel
        };

        pSectionRowPanel = new StackPanel();

        var pScroll = new ScrollViewer
        {
            Content = pSectionRowPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pHeader, Dock.Top);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pScroll);

        FocusVisualStyle = null;
        Content = PPanel.PPanelBorderBuild(pRoot);
    }

    public void PSectionAttach(PFlowControl pFlow)
    {
        PSectionDetach();
        pFlowAttached = pFlow;
        pFlowAttached.PFlowSectionChange += PSectionUpdateHandle;
        PSectionRebuild();
    }

    public IReadOnlyList<LSplitSectionDescription> PSectionSplitRead()
    {
        return pSectionListCurrent
            .Select((pSection, pIndex) => new LSplitSectionDescription(
                pSection.LSegmentStart,
                pSection.LSegmentEnd,
                PSectionNameFormat(pSection, pIndex)))
            .ToArray();
    }

    private void PSectionDetach()
    {
        if (pFlowAttached is null) return;
        pFlowAttached.PFlowSectionChange -= PSectionUpdateHandle;
        pFlowAttached = null;
    }

    private void PSectionUpdateHandle(IReadOnlyList<LSegment> pSectionList, int? pSectionIndexSelect)
    {
        pSectionListCurrent = pSectionList;
        pSectionIndexSelectCurrent = pSectionIndexSelect;
        PSectionRebuild();
    }

    private void PSectionRebuild()
    {
        pSectionRowPanel.Children.Clear();
        int pCount = pSectionListCurrent.Count;
        pSectionCountLabel.Text = pCount == 0 ? "Sections" : $"Sections  ({pCount})";
        for (int i = 0; i < pCount; i++)
        {
            pSectionRowPanel.Children.Add(PSectionRowBuild(i, pSectionListCurrent[i], i == pSectionIndexSelectCurrent));
        }
    }

    private Border PSectionRowBuild(int pSectionIndex, LSegment pSectionEntry, bool pSectionSelected)
    {
        int capturedIndex = pSectionIndex;

        Color pColor = pSectionPaletteColors[Math.Abs(pSectionEntry.LSegmentColorIndex) % pSectionPaletteColors.Length];

        var pColorDot = new Border
        {
            Width = 10,
            Height = 10,
            CornerRadius = new CornerRadius(5),
            Background = new SolidColorBrush(pColor),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var pNameBox = new TextBox
        {
            Text = PSectionNameFormat(pSectionEntry, pSectionIndex),
            FontSize = 12,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 1),
            BorderBrush = Brushes.Transparent,
            Padding = new Thickness(2, 0, 2, 1),
            VerticalAlignment = VerticalAlignment.Center,
            FocusVisualStyle = null
        };
        pNameBox.GotFocus += (_, _) =>
            pNameBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));
        pNameBox.LostFocus += (_, _) =>
        {
            pNameBox.BorderBrush = Brushes.Transparent;
            pFlowAttached?.PFlowNameSet(capturedIndex, pNameBox.Text);
        };

        var pTimeLabel = new TextBlock
        {
            Text = $"{PSectionTimeFormat(pSectionEntry.LSegmentStart)} → {PSectionTimeFormat(pSectionEntry.LSegmentEnd)}",
            FontSize = 11,
            FontFamily = pSectionFontFamily,
            Foreground = new SolidColorBrush(Color.FromRgb(0x56, 0x62, 0x73)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        var pRowContent = new Grid();
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pColorDot, 0);
        Grid.SetColumn(pNameBox, 1);
        Grid.SetColumn(pTimeLabel, 2);
        pRowContent.Children.Add(pColorDot);
        pRowContent.Children.Add(pNameBox);
        pRowContent.Children.Add(pTimeLabel);

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = pSectionSelected
                ? new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB))
                : Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
            Child = pRowContent
        };
        pRowBorder.MouseLeftButtonDown += (_, e) =>
        {
            pFlowAttached?.PFlowSectionSelect(capturedIndex);
            e.Handled = true;
        };

        return pRowBorder;
    }

    private static string PSectionNameFormat(LSegment pSectionEntry, int pSectionIndex) =>
        string.IsNullOrWhiteSpace(pSectionEntry.LSegmentName)
            ? $"Section {pSectionIndex + 1}"
            : pSectionEntry.LSegmentName;

    private static string PSectionTimeFormat(TimeSpan pTime) =>
        pTime.TotalHours >= 1
            ? $"{(int)pTime.TotalHours}:{pTime.Minutes:D2}:{pTime.Seconds:D2}"
            : $"{pTime.Minutes}:{pTime.Seconds:D2}";
}
