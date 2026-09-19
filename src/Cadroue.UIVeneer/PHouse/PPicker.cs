using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PHouse;

internal sealed class PPicker : UserControl
{
    private const double PPickerCorner = 10;
    private const double PPickerArrowWidth = 26;
    private const double PPickerPopupHeight = 260;

    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PPickerSoftBrush = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
    private static readonly Brush PPickerTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PPickerAccentBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
    private static readonly Brush PPickerMutedBrush = new SolidColorBrush(Color.FromRgb(0x9A, 0xA5, 0xB4));

    private static readonly IReadOnlyDictionary<bool, Brush> PPickerSummaryBrushes = new Dictionary<bool, Brush>
    {
        [true] = PPickerMutedBrush,
        [false] = PPickerTextBrush,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> PPickerFrameBrushes = new Dictionary<bool, Brush>
    {
        [true] = PPickerAccentBrush,
        [false] = PLineBrush,
    };

    private readonly IReadOnlyList<LPickerItem> lPickerItems;
    private readonly Dictionary<string, CheckBox> pPickerBoxes = new(StringComparer.Ordinal);
    private readonly StackPanel pPickerList = new() { Margin = new Thickness(6) };
    private readonly TextBlock pPickerSummary;
    private readonly Border pPickerFrame;
    private readonly Popup pPickerPopup;

    internal event Action? PPickerChange;

    internal PPicker(IReadOnlyList<string> pItems, IReadOnlyList<string> pSelected, string pEmptyText)
        : this(LPicker.LPickerItemsCreate(pItems, pSelected), pEmptyText)
    {
    }

    internal PPicker(IReadOnlyList<LLocalizationChoice> pItems, IReadOnlyList<string> pSelected, string pEmptyText)
        : this(LPicker.LPickerItemsCreate(pItems, pSelected), pEmptyText)
    {
    }

    private PPicker(IReadOnlyList<LPickerItem> lItems, string pEmptyText)
    {
        lPickerItems = lItems;
        PPickerEmptyText = pEmptyText;
        PScrollbar.PScrollbarApply(this);

        pPickerSummary = new TextBlock
        {
            Foreground = PPickerTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(14, 0, 10, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        ToggleButton pArrow = PPickerArrowBuild();
        lPickerItems.ToList().ForEach(PPickerRowAdd);
        pPickerPopup = PPickerPopupBuild(pArrow);

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
        PPickerCheckedRead().Select(lItem => lItem.LPickerItemToken).ToArray();

    private IEnumerable<LPickerItem> PPickerCheckedRead() => lPickerItems.Where(PPickerCheckedMatch);

    private bool PPickerCheckedMatch(LPickerItem lItem) =>
        pPickerBoxes[lItem.LPickerItemToken].IsChecked.GetValueOrDefault();

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
            Template = PPickerTemplateBuild()
        };
        PInteraction.PInteractionButtonAttach(pArrow);
        pArrow.Checked += (_, _) => PPickerOpenSet(true);
        pArrow.Unchecked += (_, _) => PPickerOpenSet(false);
        return pArrow;
    }

    private void PPickerRowAdd(LPickerItem lItem)
    {
        var pBox = new CheckBox
        {
            Content = lItem.LPickerItemLabel,
            IsChecked = PLook.PLookChecked[lItem.LPickerItemChecked],
            Margin = new Thickness(8, 6, 8, 6)
        };
        PCheckbox.PCheckboxApply(pBox);
        pBox.Checked += (_, _) => PPickerChangeHandle();
        pBox.Unchecked += (_, _) => PPickerChangeHandle();
        pPickerBoxes[lItem.LPickerItemToken] = pBox;
        pPickerList.Children.Add(pBox);
    }

    private Popup PPickerPopupBuild(ToggleButton pArrow)
    {
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
                Content = pPickerList
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
        pPickerFrame.BorderBrush = PPickerFrameBrushes[pOpen];
    }

    private void PPickerChangeHandle()
    {
        PPickerSummaryUpdate();
        PPickerChange?.Invoke();
    }

    private void PPickerSummaryUpdate()
    {
        (string lText, bool lEmpty) = LPicker.LPickerSummaryResolve(
            PPickerCheckedRead().Select(lItem => lItem.LPickerItemLabel).ToArray(),
            PPickerEmptyText);
        pPickerSummary.Text = lText;
        pPickerSummary.Foreground = PPickerSummaryBrushes[lEmpty];
    }

    private static ControlTemplate PPickerTemplateBuild()
    {
        var pTemplate = new ControlTemplate(typeof(ToggleButton));
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BorderBrushProperty, PLineBrush);
        pBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1, 0, 0, 0));
        pBorder.SetValue(Border.BackgroundProperty, PPickerSoftBrush);
        pBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, PPickerCorner, PPickerCorner, 0));

        var pArrow = new FrameworkElementFactory(typeof(Path));
        pArrow.SetValue(Shape.StrokeProperty, PPickerTextBrush);
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
