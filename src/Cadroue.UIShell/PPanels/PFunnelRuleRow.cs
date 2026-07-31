using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainArea;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed class PFunnelRuleRow : Border
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));
    private static readonly Brush pFunnelActiveBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));

    private const double PFunnelFieldHeight = 30;

    private const double PFunnelBadgeSize = 18;

    private readonly TextBlock pFunnelOrderBadge;
    private readonly Border pFunnelTitleBar;
    private Button? pFunnelFoldButton;
    private StackPanel? pFunnelBody;
    private bool pFunnelCollapsed;
    private readonly TextBox pFunnelStartField;
    private readonly TextBox pFunnelEndField;
    private readonly Border pFunnelJoinSwitch;
    private readonly ComboBox pFunnelRelayCombo;
    private readonly Func<IReadOnlyList<LCourierOption>> pFunnelOptionsRead;

    private bool pFunnelAndMode = true;
    private bool pFunnelRelayBusy;
    private Guid pFunnelTargetId;
    private int pFunnelTargetPending = -1;

    public event Action? PFunnelRowChange;
    public event Action<PFunnelRuleRow>? PFunnelRowRemove;

    public PFunnelRuleRow(Func<IReadOnlyList<LCourierOption>> pOptionsRead)
    {
        pFunnelOptionsRead = pOptionsRead;

        pFunnelOrderBadge = new TextBlock
        {
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        pFunnelStartField = PFunnelFieldBuild();
        pFunnelEndField = PFunnelFieldBuild();
        pFunnelJoinSwitch = PFunnelJoinBuild();
        pFunnelRelayCombo = PFunnelRelayBuild();

        pFunnelBody = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
        pFunnelBody.Children.Add(PFunnelLabelBuild(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.StartsWith"), pFunnelStartField));
        pFunnelBody.Children.Add(pFunnelJoinSwitch);
        pFunnelBody.Children.Add(PFunnelLabelBuild(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.EndsWith"), pFunnelEndField));
        pFunnelBody.Children.Add(PFunnelTargetBuild());

        var pCard = new DockPanel { LastChildFill = true };
        pFunnelTitleBar = PFunnelTitleBuild();
        DockPanel.SetDock(pFunnelTitleBar, Dock.Top);
        pCard.Children.Add(pFunnelTitleBar);
        pCard.Children.Add(pFunnelBody);

        Margin = new Thickness(0, 0, 0, 10);
        Background = Brushes.White;
        BorderBrush = pFunnelLineBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        SnapsToDevicePixels = true;
        Child = pCard;

        PFunnelJoinUpdate();
        PFunnelRelayRebuild();
    }

    public Border PFunnelHeader => pFunnelTitleBar;

    public void PFunnelOrderSet(int pOrder)
    {
        pFunnelOrderBadge.Text = pOrder.ToString();
    }

    public void PFunnelSelectSet(bool pSelected)
    {
        pFunnelTitleBar.Background = pSelected ? pFunnelActiveBrush : Brushes.Transparent;
    }

    public string PFunnelRowStart => pFunnelStartField.Text.Trim();

    public string PFunnelRowEnd => pFunnelEndField.Text.Trim();

    public bool PFunnelRowAnd => pFunnelAndMode;

    public Guid PFunnelTargetId => pFunnelTargetId;

    public int PFunnelTargetPending => pFunnelTargetPending;

    public void PFunnelRowRestore(string pStart, string pEnd, bool pAndMode, int pTargetIndex)
    {
        pFunnelStartField.Text = pStart;
        pFunnelEndField.Text = pEnd;
        pFunnelAndMode = pAndMode;
        pFunnelTargetPending = pTargetIndex;
        PFunnelJoinUpdate();
    }

    public void PFunnelTargetSet(Guid pTargetId)
    {
        pFunnelTargetId = pTargetId;
        pFunnelTargetPending = -1;
        PFunnelRelayRebuild();
    }

    public bool PFunnelRowMatch(string pFileName)
    {
        string pStart = PFunnelRowStart;
        string pEnd = PFunnelRowEnd;
        if (pStart.Length == 0 && pEnd.Length == 0)
        {
            return false;
        }

        bool pStartHas = pStart.Length > 0;
        bool pEndHas = pEnd.Length > 0;
        bool pStartOk = !pStartHas || pFileName.StartsWith(pStart, StringComparison.OrdinalIgnoreCase);
        bool pEndOk = !pEndHas || pFileName.EndsWith(pEnd, StringComparison.OrdinalIgnoreCase);

        if (pStartHas && pEndHas)
        {
            return pFunnelAndMode ? pStartOk && pEndOk : pStartOk || pEndOk;
        }

        return pStartOk && pEndOk;
    }

    private static TextBox PFunnelFieldBuild()
    {
        var pField = new TextBox
        {
            Height = PFunnelFieldHeight,
            FontSize = 12,
            FontFamily = pFunnelFontFamily
        };
        PTextbox.PTextboxApply(pField);
        return pField;
    }

    private Border PFunnelJoinBuild()
    {
        var pHost = new Border
        {
            Margin = new Thickness(0, 6, 0, 6),
            HorizontalAlignment = HorizontalAlignment.Center,
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            SnapsToDevicePixels = true
        };
        return pHost;
    }

    private void PFunnelJoinUpdate()
    {
        var pRow = new StackPanel { Orientation = Orientation.Horizontal };
        pRow.Children.Add(PFunnelSegmentBuild(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.And"), pFunnelAndMode, () => PFunnelModeSet(true)));
        pRow.Children.Add(new Border { Width = 1, Background = pFunnelLineBrush });
        pRow.Children.Add(PFunnelSegmentBuild(
            LLocalization.LLocalizationTextRead("Inspector.Funnel.Or"), !pFunnelAndMode, () => PFunnelModeSet(false)));
        pFunnelJoinSwitch.Child = pRow;
    }

    private static Border PFunnelSegmentBuild(string pText, bool pActive, Action pClick)
    {
        var pLabel = new TextBlock
        {
            Text = pText,
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            FontWeight = pActive ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = pActive ? pFunnelTitleBrush : pFunnelMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var pSegment = new Border
        {
            Background = pActive ? pFunnelActiveBrush : Brushes.Transparent,
            Padding = new Thickness(14, 3, 14, 3),
            Cursor = System.Windows.Input.Cursors.Hand,
            Child = pLabel
        };
        pSegment.MouseLeftButtonUp += (_, _) => pClick();
        return pSegment;
    }

    private void PFunnelModeSet(bool pAndMode)
    {
        if (pFunnelAndMode == pAndMode)
        {
            return;
        }

        pFunnelAndMode = pAndMode;
        PFunnelJoinUpdate();
        PFunnelRowChange?.Invoke();
    }

    private ComboBox PFunnelRelayBuild()
    {
        var pCombo = new ComboBox
        {
            Height = PFunnelFieldHeight,
            MinWidth = 120,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            FocusVisualStyle = null,
            SelectedValuePath = "LCourierTabId",
            ItemTemplate = PFunnelTemplateBuild()
        };
        PDropdown.PDropdownApply(pCombo);
        pCombo.DropDownOpened += (_, _) => PFunnelRelayRebuild();
        pCombo.SelectionChanged += PFunnelRelayHandle;
        return pCombo;
    }

    private void PFunnelRelayRebuild()
    {
        pFunnelRelayBusy = true;
        var pOptions = new List<LCourierOption>
        {
            new(Guid.Empty, LLocalization.LLocalizationTextRead("Inspector.Funnel.RelayNone"), null)
        };
        pOptions.AddRange(pFunnelOptionsRead());
        if (pFunnelTargetId != Guid.Empty && pOptions.All(pOption => pOption.LCourierTabId != pFunnelTargetId))
        {
            pFunnelTargetId = Guid.Empty;
        }

        pFunnelRelayCombo.ItemsSource = pOptions;
        pFunnelRelayCombo.SelectedValue = pFunnelTargetId;
        pFunnelRelayBusy = false;
    }

    private void PFunnelRelayHandle(object pSender, SelectionChangedEventArgs pArgs)
    {
        if (pFunnelRelayBusy || pFunnelRelayCombo.SelectedValue is not Guid pTargetId)
        {
            return;
        }

        pFunnelTargetId = pTargetId;
        pFunnelTargetPending = -1;
        PFunnelRowChange?.Invoke();
    }

    private static DataTemplate PFunnelTemplateBuild()
    {
        var pStack = new FrameworkElementFactory(typeof(StackPanel));
        pStack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

        var pIconStyle = new Style(typeof(Image));
        var pIconTrigger = new DataTrigger { Binding = new Binding("LCourierTabIcon"), Value = null };
        pIconTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        pIconStyle.Triggers.Add(pIconTrigger);

        var pIcon = new FrameworkElementFactory(typeof(Image));
        pIcon.SetValue(FrameworkElement.WidthProperty, 14.0);
        pIcon.SetValue(FrameworkElement.HeightProperty, 14.0);
        pIcon.SetValue(Image.StretchProperty, Stretch.Uniform);
        pIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 6, 0));
        pIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pIcon.SetValue(FrameworkElement.StyleProperty, pIconStyle);
        pIcon.SetBinding(Image.SourceProperty, new Binding("LCourierTabIcon"));

        var pText = new FrameworkElementFactory(typeof(TextBlock));
        pText.SetValue(TextBlock.FontSizeProperty, 12.0);
        pText.SetValue(TextBlock.FontFamilyProperty, pFunnelFontFamily);
        pText.SetValue(TextBlock.ForegroundProperty, pFunnelTitleBrush);
        pText.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pText.SetBinding(TextBlock.TextProperty, new Binding("LCourierTabTitle"));

        pStack.AppendChild(pIcon);
        pStack.AppendChild(pText);
        return new DataTemplate { VisualTree = pStack };
    }

    private Grid PFunnelLabelBuild(string pLabel, UIElement pField)
    {
        var pRow = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        pRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pRow.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var pLabelBlock = new TextBlock
        {
            Text = pLabel,
            FontSize = 11,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelMutedBrush,
            Margin = new Thickness(2, 0, 0, 3)
        };
        Grid.SetRow(pLabelBlock, 0);
        Grid.SetRow(pField, 1);
        pRow.Children.Add(pLabelBlock);
        pRow.Children.Add(pField);
        return pRow;
    }

    private Grid PFunnelTargetBuild()
    {
        var pRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pRelayLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Funnel.Relay"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelTitleBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(2, 0, 8, 0)
        };

        Grid.SetColumn(pRelayLabel, 0);
        Grid.SetColumn(pFunnelRelayCombo, 1);
        pRow.Children.Add(pRelayLabel);
        pRow.Children.Add(pFunnelRelayCombo);
        return pRow;
    }

    private Border PFunnelTitleBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Funnel.Filename"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pFunnelTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pBadge = new Border
        {
            MinWidth = PFunnelBadgeSize,
            Height = PFunnelBadgeSize,
            CornerRadius = new CornerRadius(PFunnelBadgeSize / 2),
            Background = pFunnelAccentBrush,
            Padding = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Child = pFunnelOrderBadge
        };

        var pBar = new DockPanel { LastChildFill = true };
        Button pRemoveButton = PFunnelRemoveBuild();
        pFunnelFoldButton = PFunnelFoldBuild();
        DockPanel.SetDock(pRemoveButton, Dock.Right);
        DockPanel.SetDock(pFunnelFoldButton, Dock.Right);
        DockPanel.SetDock(pBadge, Dock.Left);
        pBar.Children.Add(pRemoveButton);
        pBar.Children.Add(pFunnelFoldButton);
        pBar.Children.Add(pBadge);
        pBar.Children.Add(pTitleLabel);

        return new Border
        {
            Padding = new Thickness(10, 4, 6, 4),
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.Transparent,
            Cursor = System.Windows.Input.Cursors.Hand,
            Child = pBar
        };
    }

    private Button PFunnelFoldBuild()
    {
        var pButton = new Button
        {
            Width = 20,
            Height = 20,
            Padding = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Style = PButton.PButtonChromeCreate(false),
            Content = PFunnelGlyphCreate(false),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Funnel.Minimize")
        };
        pButton.Click += (_, _) => PFunnelFoldToggle();
        return pButton;
    }

    private void PFunnelFoldToggle()
    {
        pFunnelCollapsed = !pFunnelCollapsed;
        if (pFunnelBody is { } pBody)
        {
            pBody.Visibility = pFunnelCollapsed ? Visibility.Collapsed : Visibility.Visible;
        }

        pFunnelTitleBar.BorderThickness = new Thickness(0, 0, 0, pFunnelCollapsed ? 0 : 1);

        if (pFunnelFoldButton is { } pFold)
        {
            pFold.Content = PFunnelGlyphCreate(pFunnelCollapsed);
            pFold.ToolTip = LLocalization.LLocalizationTextRead(
                pFunnelCollapsed ? "Inspector.Funnel.Maximize" : "Inspector.Funnel.Minimize");
        }
    }

    private static UIElement PFunnelGlyphCreate(bool pCollapsed)
    {
        var pCanvas = new Canvas
        {
            Width = 12,
            Height = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (pCollapsed)
        {
            var pRect = new System.Windows.Shapes.Rectangle
            {
                Width = 9,
                Height = 9,
                Stroke = pFunnelMutedBrush,
                StrokeThickness = 1.2,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(pRect, 1.5);
            Canvas.SetTop(pRect, 1.5);
            pCanvas.Children.Add(pRect);
        }
        else
        {
            pCanvas.Children.Add(new System.Windows.Shapes.Line
            {
                X1 = 1.5,
                Y1 = 9,
                X2 = 10.5,
                Y2 = 9,
                Stroke = pFunnelMutedBrush,
                StrokeThickness = 1.25,
                StrokeStartLineCap = PenLineCap.Square,
                StrokeEndLineCap = PenLineCap.Square
            });
        }

        return pCanvas;
    }

    private Button PFunnelRemoveBuild()
    {
        var pButton = new Button
        {
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Content = "×",
            FontSize = 15,
            Foreground = pFunnelMutedBrush,
            Padding = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Style = PButton.PButtonChromeCreate(false),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Funnel.Remove")
        };
        pButton.Click += (_, _) => PFunnelRowRemove?.Invoke(this);
        return pButton;
    }
}
