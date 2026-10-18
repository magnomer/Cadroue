using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal sealed class PFunnelRuleRelay
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    private const double PFunnelFieldHeight = 30;

    private readonly QFunnelRelay lFunnelRelay;
    private readonly QFunnelRule lFunnelRule;
    private readonly ComboBox pFunnelRelayCombo;

    public PFunnelRuleRelay(QFunnelRelay lRelay, QFunnelRule lRule)
    {
        lFunnelRelay = lRelay;
        lFunnelRule = lRule;
        pFunnelRelayCombo = PFunnelRelayBuild();
    }

    public void PFunnelRelayUpdate()
    {
        PFunnelSelectionDetach();
        PFunnelRelayShow();
        PFunnelSelectionAttach();
    }

    public Grid PFunnelTargetBuild()
    {
        var pRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pRelayLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Relay"),
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

    private ComboBox PFunnelRelayBuild()
    {
        ComboBox pCombo = PFunnelRelayCreate();
        QDropdown.QDropdownAttach(pCombo);
        PFunnelRelayAttach(pCombo);
        return pCombo;
    }

    private static ComboBox PFunnelRelayCreate() => new()
    {
        Height = PFunnelFieldHeight,
        MinWidth = 120,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalContentAlignment = VerticalAlignment.Center,
        FontSize = 12,
        FontFamily = pFunnelFontFamily,
        FocusVisualStyle = null,
        SelectedValuePath = "PActionRelayId",
        ItemTemplate = PFunnelTemplateBuild()
    };

    private void PFunnelRelayAttach(ComboBox pCombo)
    {
        pCombo.DropDownOpened += (_, _) => PFunnelRelayUpdate();
        pCombo.SelectionChanged += PFunnelRelayHandle;
    }

    private void PFunnelSelectionDetach() => pFunnelRelayCombo.SelectionChanged -= PFunnelRelayHandle;

    private void PFunnelSelectionAttach() => pFunnelRelayCombo.SelectionChanged += PFunnelRelayHandle;

    private void PFunnelRelayShow()
    {
        pFunnelRelayCombo.ItemsSource = PFunnelOptionsResolve();
        pFunnelRelayCombo.SelectedValue = lFunnelRule.QFunnelRuleTarget;
    }

    private List<PActionRelayOption> PFunnelOptionsResolve() =>
        lFunnelRelay.QFunnelOptionsRead().Select(PFunnelOptionBuild).ToList();

    private static PActionRelayOption PFunnelOptionBuild(QFunnelTarget lTarget) =>
        new(lTarget.QFunnelTargetId, lTarget.QFunnelTargetTitle, QStripIcon.QStripIconFind(lTarget.QFunnelTargetKey));

    private void PFunnelRelayHandle(object pSender, SelectionChangedEventArgs pArgs) =>
        lFunnelRelay.QFunnelTargetSelect(lFunnelRule, pFunnelRelayCombo.SelectedIndex);

    private static DataTemplate PFunnelTemplateBuild()
    {
        var pStack = new FrameworkElementFactory(typeof(StackPanel));
        pStack.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

        var pIconStyle = new Style(typeof(Image));
        var pIconTrigger = new DataTrigger { Binding = new Binding("PActionRelayIcon"), Value = null };
        pIconTrigger.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
        pIconStyle.Triggers.Add(pIconTrigger);

        var pIcon = new FrameworkElementFactory(typeof(Image));
        pIcon.SetValue(FrameworkElement.WidthProperty, 14.0);
        pIcon.SetValue(FrameworkElement.HeightProperty, 14.0);
        pIcon.SetValue(Image.StretchProperty, Stretch.Uniform);
        pIcon.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 6, 0));
        pIcon.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pIcon.SetValue(FrameworkElement.StyleProperty, pIconStyle);
        pIcon.SetBinding(Image.SourceProperty, new Binding("PActionRelayIcon"));

        var pText = new FrameworkElementFactory(typeof(TextBlock));
        pText.SetValue(TextBlock.FontSizeProperty, 12.0);
        pText.SetValue(TextBlock.FontFamilyProperty, pFunnelFontFamily);
        pText.SetValue(TextBlock.ForegroundProperty, pFunnelTitleBrush);
        pText.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
        pText.SetBinding(TextBlock.TextProperty, new Binding("PActionRelayTitle"));

        pStack.AppendChild(pIcon);
        pStack.AppendChild(pText);
        return new DataTemplate { VisualTree = pStack };
    }
}
