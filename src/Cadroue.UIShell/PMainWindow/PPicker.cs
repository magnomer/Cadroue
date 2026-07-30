using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cadroue.UIShell.PMainWindow;

internal sealed class PPicker : UserControl
{
    private const double PPickerCorner = 10;
    private const double PPickerArrowWidth = 26;
    private const double PPickerPopupHeight = 260;

    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
    private static readonly Brush PMutedBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0xA5, 0xB4));

    private readonly string[] pPickerItems;
    private readonly IReadOnlyDictionary<string, string> pPickerLabels;
    private readonly Dictionary<string, CheckBox> pPickerBoxes = new(StringComparer.Ordinal);
    private readonly TextBlock pPickerSummary;
    private readonly Border pPickerFrame;
    private readonly Popup pPickerPopup;

    internal PPicker(IReadOnlyList<string> pItems, IReadOnlyList<string> pSelected, string pEmptyText)
        : this(pItems, pSelected, pEmptyText, new Dictionary<string, string>(StringComparer.Ordinal))
    {
    }

    internal PPicker(IReadOnlyList<LLocalizationChoice> pItems, IReadOnlyList<string> pSelected, string pEmptyText)
        : this(
            pItems.Select(pItem => pItem.LLocalizationChoiceToken).ToArray(),
            pSelected,
            pEmptyText,
            pItems.ToDictionary(
                pItem => pItem.LLocalizationChoiceToken,
                pItem => pItem.ToString(),
                StringComparer.Ordinal))
    {
    }

    private PPicker(
        IReadOnlyList<string> pItems,
        IReadOnlyList<string> pSelected,
        string pEmptyText,
        IReadOnlyDictionary<string, string> pLabels)
    {
        pPickerItems = pItems.ToArray();
        pPickerLabels = pLabels;
        PPickerEmptyText = pEmptyText;

        pPickerSummary = new TextBlock
        {
            Foreground = PTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 10, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        ToggleButton pArrow = PPickerArrowBuild();
        pPickerPopup = PPickerPopupBuild(pArrow, pSelected);

        var pDock = new DockPanel();
        DockPanel.SetDock(pArrow, Dock.Right);
        pDock.Children.Add(pArrow);
        pDock.Children.Add(pPickerSummary);

        pPickerFrame = new Border
        {
            Background = Brushes.White,
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(PPickerCorner),
            Child = pDock
        };

        var pRoot = new Grid();
        pRoot.Children.Add(pPickerFrame);
        pRoot.Children.Add(pPickerPopup);
        Content = pRoot;
        Focusable = false;
        PPickerSummaryUpdate();
    }

    internal string PPickerEmptyText { get; }

    internal IReadOnlyList<string> PPickerSelectionRead() =>
        pPickerItems.Where(pItem => pPickerBoxes[pItem].IsChecked == true).ToArray();

    private ToggleButton PPickerArrowBuild()
    {
        var pArrow = new ToggleButton
        {
            Width = PPickerArrowWidth,
            Focusable = false,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            ClickMode = ClickMode.Press,
            Cursor = Cursors.Hand,
            Template = PPickerArrowTemplateBuild()
        };
        pArrow.Checked += (_, _) => PPickerOpenSet(true);
        pArrow.Unchecked += (_, _) => PPickerOpenSet(false);
        return pArrow;
    }

    private Popup PPickerPopupBuild(ToggleButton pArrow, IReadOnlyList<string> pSelected)
    {
        var pList = new StackPanel { Margin = new Thickness(6) };
        foreach (string pItem in pPickerItems)
        {
            var pBox = new CheckBox
            {
                Content = PPickerLabelRead(pItem),
                IsChecked = pSelected.Contains(pItem, StringComparer.Ordinal),
                Margin = new Thickness(8, 6, 8, 6)
            };
            PCheckbox.PCheckboxApply(pBox);
            pBox.Checked += (_, _) => PPickerSummaryUpdate();
            pBox.Unchecked += (_, _) => PPickerSummaryUpdate();
            pPickerBoxes[pItem] = pBox;
            pList.Children.Add(pBox);
        }

        var pCard = new Border
        {
            Background = Brushes.White,
            BorderBrush = PLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(PPickerCorner),
            Margin = new Thickness(0, 6, 0, 0),
            Child = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MaxHeight = PPickerPopupHeight,
                Content = pList
            }
        };
        pCard.SetBinding(MinWidthProperty, new System.Windows.Data.Binding("ActualWidth") { Source = this });

        var pPopup = new Popup
        {
            Placement = PlacementMode.Bottom,
            PlacementTarget = this,
            AllowsTransparency = true,
            StaysOpen = false,
            Child = pCard
        };
        pPopup.Closed += (_, _) =>
        {
            pArrow.IsChecked = false;
            pPickerFrame.BorderBrush = PLineBrush;
        };
        return pPopup;
    }

    private void PPickerOpenSet(bool pOpen)
    {
        pPickerPopup.IsOpen = pOpen;
        pPickerFrame.BorderBrush = pOpen ? PAccentBrush : PLineBrush;
    }

    private void PPickerSummaryUpdate()
    {
        IReadOnlyList<string> pSelection = PPickerSelectionRead();
        pPickerSummary.Text = pSelection.Count == 0
            ? PPickerEmptyText
            : string.Join(", ", pSelection.Select(PPickerLabelRead));
        pPickerSummary.Foreground = pSelection.Count == 0 ? PMutedBrush : PTextBrush;
    }

    private string PPickerLabelRead(string pItem) =>
        pPickerLabels.TryGetValue(pItem, out string? pLabel) ? pLabel : pItem;

    private static ControlTemplate PPickerArrowTemplateBuild()
    {
        var pTemplate = new ControlTemplate(typeof(ToggleButton));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BorderBrushProperty, PLineBrush);
        pBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1, 0, 0, 0));
        pBorder.SetValue(Border.BackgroundProperty, PSoftBrush);
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, PPickerCorner, PPickerCorner, 0));

        var pArrow = new FrameworkElementFactory(typeof(Path));
        pArrow.SetValue(Shape.StrokeProperty, PTextBrush);
        pArrow.SetValue(Shape.StrokeThicknessProperty, 1.3);
        pArrow.SetValue(Shape.StrokeStartLineCapProperty, PenLineCap.Round);
        pArrow.SetValue(Shape.StrokeEndLineCapProperty, PenLineCap.Round);
        pArrow.SetValue(Shape.StrokeLineJoinProperty, PenLineJoin.Round);
        pArrow.SetValue(Path.DataProperty, Geometry.Parse("M 3 4 L 6 7 L 9 4"));
        pArrow.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        pArrow.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pBorder.AppendChild(pArrow);

        pTemplate.VisualTree = pBorder;
        return pTemplate;
    }
}
