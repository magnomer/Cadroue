using Cadroue.Application;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Cadroue.UIVeneer.PCabin;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PPorch;

namespace Cadroue.UIVeneer.PWing;

public sealed class PFunnelRuleRow : Border
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    private const double PFunnelFieldHeight = 30;

    private readonly LFunnel lFunnel;
    private readonly List<PFunnelCondition> pFunnelConditions;
    private readonly ComboBox pFunnelRelayCombo;
    private readonly PFunnelRuleFrame pFunnelFrame;
    private readonly StackPanel pFunnelRegexStack;
    private readonly TextBox pFunnelRegexField;
    private readonly CheckBox pFunnelWholeBox;

    public PFunnelRuleRow(LFunnel lFunnelOwner, LFunnelRule lFunnelRule)
    {
        lFunnel = lFunnelOwner;
        PFunnelRule = lFunnelRule;
        pFunnelRelayCombo = PFunnelRelayBuild();
        pFunnelRegexField = PFunnelRegexBuild();
        pFunnelWholeBox = PFunnelWholeBuild();
        pFunnelRegexStack = new StackPanel
        {
            Visibility = PLook.PLookVisible[lFunnelRule.LFunnelRulePattern]
        };
        pFunnelRegexStack.Children.Add(pFunnelRegexField);
        pFunnelRegexStack.Children.Add(pFunnelWholeBox);
        pFunnelConditions = LFunnel.LFunnelConditions.Select(PFunnelConditionBuild).ToList();

        var pBody = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
        pBody.Children.Add(pFunnelRegexStack);
        pFunnelConditions.ForEach(pCondition => pBody.Children.Add(pCondition));
        pBody.Children.Add(PFunnelTargetBuild());

        pFunnelFrame = new PFunnelRuleFrame(
            pBody,
            lFunnelRule.LFunnelRuleTitle,
            () => lFunnelOwner.LFunnelRuleRemove(lFunnelRule),
            () => lFunnelOwner.LFunnelCollapsedToggle(lFunnelRule));

        var pCard = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pFunnelFrame.PFunnelHeader, Dock.Top);
        pCard.Children.Add(pFunnelFrame.PFunnelHeader);
        pCard.Children.Add(pBody);

        Margin = new Thickness(0, 0, 0, 10);
        Background = Brushes.White;
        BorderBrush = pFunnelLineBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        SnapsToDevicePixels = true;
        Child = pCard;

        PFunnelRowUpdate();
    }

    public LFunnelRule PFunnelRule { get; }

    public Border PFunnelHeader => pFunnelFrame.PFunnelHeader;

    public void PFunnelOrderSet(int pOrder) => pFunnelFrame.PFunnelOrderSet(pOrder);

    public void PFunnelSelectSet(bool pSelected) => pFunnelFrame.PFunnelSelectSet(pSelected);

    public void PFunnelRowUpdate()
    {
        pFunnelFrame.PFunnelCollapsedSet(PFunnelRule.LFunnelRuleCollapsed);
        pFunnelRegexField.Text = lFunnel.LFunnelRegexResolve(PFunnelRule, pFunnelRegexField.Text);
        pFunnelWholeBox.IsChecked = PFunnelRule.LFunnelRuleWhole;
        pFunnelConditions.ForEach(PFunnelConditionApply);
        PFunnelRelayUpdate();
    }

    private static void PFunnelConditionApply(PFunnelCondition pCondition) => pCondition.PFunnelConditionUpdate();

    private PFunnelCondition PFunnelConditionBuild(LFunnelCondition lCondition) =>
        new(lFunnel, PFunnelRule, lCondition)
        {
            Visibility = PLook.PLookVisible[PFunnelRule.LFunnelRuleFields]
        };

    private TextBox PFunnelRegexBuild()
    {
        var pField = new TextBox
        {
            Height = PFunnelFieldHeight,
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Margin = new Thickness(0, 0, 0, 8)
        };
        PTextbox.PTextboxApply(pField);
        pField.TextChanged += (_, _) => lFunnel.LFunnelRegexSet(PFunnelRule, pField.Text);
        return pField;
    }

    private CheckBox PFunnelWholeBuild()
    {
        var pBox = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Funnel.Whole"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelTitleBrush,
            Margin = new Thickness(2, 0, 0, 0)
        };
        PCheckbox.PCheckboxApply(pBox);
        pBox.Checked += (_, _) => lFunnel.LFunnelWholeSet(PFunnelRule, true);
        pBox.Unchecked += (_, _) => lFunnel.LFunnelWholeSet(PFunnelRule, false);
        return pBox;
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
            SelectedValuePath = "PActionRelayId",
            ItemTemplate = PFunnelTemplateBuild()
        };
        PDropdown.PDropdownApply(pCombo);
        pCombo.DropDownOpened += (_, _) => PFunnelRelayUpdate();
        pCombo.SelectionChanged += PFunnelRelayHandle;
        return pCombo;
    }

    private void PFunnelRelayUpdate()
    {
        pFunnelRelayCombo.SelectionChanged -= PFunnelRelayHandle;
        pFunnelRelayCombo.ItemsSource = lFunnel.LFunnelOptionsRead().Select(PFunnelOptionBuild).ToList();
        pFunnelRelayCombo.SelectedValue = PFunnelRule.LFunnelRuleTarget;
        pFunnelRelayCombo.SelectionChanged += PFunnelRelayHandle;
    }

    private static PActionRelayOption PFunnelOptionBuild(LFunnelTarget lTarget) =>
        new(lTarget.LFunnelTargetId, lTarget.LFunnelTargetTitle, PTabIcon.PTabIconFind(lTarget.LFunnelTargetKey));

    private void PFunnelRelayHandle(object pSender, SelectionChangedEventArgs pArgs) =>
        lFunnel.LFunnelTargetSelect(PFunnelRule, pFunnelRelayCombo.SelectedIndex);

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
}
