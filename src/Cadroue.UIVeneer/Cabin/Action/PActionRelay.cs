using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PActionRelay
{
    private static readonly Brush pActionRelayText = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pActionRelayLine = new SolidColorBrush(Color.FromRgb(0xC9, 0xD6, 0xE5));
    private static readonly Brush pActionRelayHover = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFC));
    private static readonly Brush pActionHoverLine = new SolidColorBrush(Color.FromRgb(0xD5, 0xE0, 0xED));

    private readonly QAction lAction;
    private readonly Image pActionRelayIcon;
    private readonly TextBlock pActionRelayLabel;

    public PActionRelay(QAction lActionOwner)
    {
        lAction = lActionOwner;
        pActionRelayIcon = PActionIconBuild();
        pActionRelayLabel = PActionLabelBuild();
        PActionRelayButton = PActionRelayBuild();
        PActionAutoBox = PActionAutoBuild();
    }

    public Button PActionRelayButton { get; }

    public CheckBox PActionAutoBox { get; }

    public void PActionRelayHide() => PActionRelayButton.Visibility = Visibility.Collapsed;

    public void PActionAutoApply(bool lActionAutoRelay)
    {
        lAction.QActionAutoSet(lActionAutoRelay);
        PActionBoxSet(lActionAutoRelay);
    }

    public void PActionFaceUpdate()
    {
        pActionRelayLabel.Text = lAction.QActionFaceText;
        pActionRelayIcon.Source = QStripIcon.QStripIconFind(lAction.QActionFaceIcon);
        pActionRelayIcon.Visibility = PLook.PLookVisible[lAction.QActionFaceShown];
    }

    private void PActionBoxSet(bool lActionAutoRelay) => PActionAutoBox.IsChecked = lActionAutoRelay;

    private CheckBox PActionAutoBuild()
    {
        CheckBox pAutoBox = PActionBoxBuild();
        QInteractionClick.QInteractionClickAttach(pAutoBox);
        QCheckbox.QCheckboxAttach(pAutoBox);
        PActionBoxAttach(pAutoBox);
        return pAutoBox;
    }

    private static CheckBox PActionBoxBuild()
    {
        return new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Action.AutoRelay.Label"),
            FontSize = 12,
            Foreground = pActionRelayText,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0),
            FocusVisualStyle = null,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Action.AutoRelay.Tooltip")
        };
    }

    private void PActionBoxAttach(CheckBox pAutoBox)
    {
        pAutoBox.Checked += (_, _) => lAction.QActionAutoSet(pAutoBox.IsChecked);
        pAutoBox.Unchecked += (_, _) => lAction.QActionAutoSet(pAutoBox.IsChecked);
    }

    private Button PActionRelayBuild()
    {
        Button pButton = PActionFrameBuild();
        PActionOpenAttach(pButton);
        return pButton;
    }

    private void PActionOpenAttach(Button pButton) => pButton.Click += PActionOpenHandle;

    private Button PActionFrameBuild()
    {
        var pChevron = new System.Windows.Shapes.Path
        {
            Stroke = pActionRelayText,
            StrokeThickness = 1.3,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            Data = Geometry.Parse("M 3 4 L 6 7 L 9 4"),
            Width = 9,
            Height = 6,
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };

        var pFace = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pActionRelayIcon, Dock.Left);
        DockPanel.SetDock(pChevron, Dock.Right);
        pFace.Children.Add(pActionRelayIcon);
        pFace.Children.Add(pChevron);
        pFace.Children.Add(pActionRelayLabel);

        var pButton = new Button
        {
            Height = 42,
            MinWidth = 142,
            MaxWidth = 190,
            Padding = new Thickness(11, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Background = Brushes.White,
            BorderBrush = pActionRelayLine,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            FocusVisualStyle = null,
            Content = pFace,
            Template = PActionTemplateBuild(),
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Action.Relay.Tooltip")
        };
        System.Windows.Automation.AutomationProperties.SetName(
            pButton, QLocalizationCatalog.QLocalizationTextRead("Action.Relay.Name"));
        return pButton;
    }

    private static ControlTemplate PActionTemplateBuild()
    {
        var pTemplate = new ControlTemplate(typeof(Button));
        var pFrame = new FrameworkElementFactory(typeof(Border));
        pFrame.Name = "pActionRelayFrame";
        pFrame.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pFrame.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Control.BorderBrushProperty));
        pFrame.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Control.BorderThicknessProperty));
        pFrame.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
        pFrame.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
        pFrame.SetValue(UIElement.SnapsToDevicePixelsProperty, true);

        var pContent = new FrameworkElementFactory(typeof(ContentPresenter));
        pContent.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pFrame.AppendChild(pContent);
        pTemplate.VisualTree = pFrame;

        var pHover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        pHover.Setters.Add(new Setter(Border.BackgroundProperty, pActionRelayHover, "pActionRelayFrame"));
        pHover.Setters.Add(new Setter(Border.BorderBrushProperty, pActionHoverLine, "pActionRelayFrame"));
        pTemplate.Triggers.Add(pHover);
        return pTemplate;
    }

    private static Image PActionIconBuild() => new()
    {
        Width = 17,
        Height = 17,
        Stretch = Stretch.Uniform,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, 0, 10, 0),
        Visibility = Visibility.Collapsed
    };

    private static TextBlock PActionLabelBuild() => new()
    {
        FontSize = 12,
        Foreground = pActionRelayText,
        VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    };

    private void PActionOpenHandle(object pSender, RoutedEventArgs pArgs)
    {
        ContextMenu pRelayMenu = QMenu.QMenuBuild(PActionRelayButton, QMenuSide.QMenuSideBelow);
        lAction.QActionMenuRead().ToList().ForEach(lOption => PActionRowAppend(pRelayMenu, lOption));
        PActionMenuShow(pRelayMenu, pArgs);
    }

    private static void PActionMenuShow(ContextMenu pRelayMenu, RoutedEventArgs pArgs)
    {
        pRelayMenu.IsOpen = true;
        pArgs.Handled = true;
    }

    private void PActionRowAppend(ContextMenu pRelayMenu, QActionOption lOption)
    {
        MenuItem pRelayItem = QMenu.QMenuItemBuild(
            lOption.QActionOptionTitle,
            QStripIcon.QStripIconFind(lOption.QActionOptionKey));
        PActionItemAttach(pRelayItem, lOption);
        pRelayMenu.Items.Add(pRelayItem);
    }

    private void PActionItemAttach(MenuItem pRelayItem, QActionOption lOption) =>
        pRelayItem.Click += (_, _) => lAction.QActionRelaySelect(lOption.QActionOptionId);
}
