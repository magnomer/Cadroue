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

    private readonly QFunnel lFunnel;
    private readonly List<PFunnelCondition> pFunnelConditions;
    private readonly ComboBox pFunnelRelayCombo;
    private readonly PFunnelRuleFrame pFunnelFrame;
    private readonly StackPanel pFunnelRegexStack;
    private readonly TextBox pFunnelRegexField;
    private readonly CheckBox pFunnelWholeBox;

    public PFunnelRuleRow(QFunnel lFunnelOwner, QFunnelRule lFunnelRule)
    {
        lFunnel = lFunnelOwner;
        PFunnelRule = lFunnelRule;
        pFunnelRelayCombo = PFunnelRelayBuild();
        pFunnelRegexField = PFunnelRegexBuild();
        pFunnelWholeBox = PFunnelWholeBuild();
        pFunnelRegexStack = PFunnelStackBuild(lFunnelRule.QFunnelRulePattern);
        pFunnelConditions = QFunnel.QFunnelConditions.Select(PFunnelConditionBuild).ToList();
        StackPanel pBody = PFunnelBodyBuild();

        pFunnelFrame = new PFunnelRuleFrame(
            pBody,
            lFunnelRule.QFunnelRuleTitle,
            () => lFunnelOwner.QFunnelRuleRemove(lFunnelRule),
            () => lFunnelOwner.QFunnelCollapsedToggle(lFunnelRule));

        PFunnelBorderApply();
        PFunnelCardBuild(pBody);
        PFunnelRowUpdate();
    }

    private StackPanel PFunnelStackBuild(bool pPattern)
    {
        var pStack = new StackPanel
        {
            Visibility = PLook.PLookVisible[pPattern]
        };
        pStack.Children.Add(pFunnelRegexField);
        pStack.Children.Add(pFunnelWholeBox);
        return pStack;
    }

    private StackPanel PFunnelBodyBuild()
    {
        var pBody = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
        pBody.Children.Add(pFunnelRegexStack);
        pFunnelConditions.ForEach(pCondition => pBody.Children.Add(pCondition));
        pBody.Children.Add(PFunnelTargetBuild());
        return pBody;
    }

    private void PFunnelBorderApply()
    {
        Margin = new Thickness(0, 0, 0, 10);
        Background = Brushes.White;
        BorderBrush = pFunnelLineBrush;
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        SnapsToDevicePixels = true;
    }

    private void PFunnelCardBuild(StackPanel pBody)
    {
        var pCard = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(pFunnelFrame.PFunnelHeader, Dock.Top);
        pCard.Children.Add(pFunnelFrame.PFunnelHeader);
        pCard.Children.Add(pBody);
        Child = pCard;
    }

    public QFunnelRule PFunnelRule { get; }

    public Border PFunnelHeader => pFunnelFrame.PFunnelHeader;

    public void PFunnelOrderSet(int pOrder) => pFunnelFrame.PFunnelOrderSet(pOrder);

    public void PFunnelSelectSet(bool pSelected) => pFunnelFrame.PFunnelSelectSet(pSelected);

    public void PFunnelRowUpdate()
    {
        pFunnelFrame.PFunnelCollapsedSet(PFunnelRule.QFunnelRuleCollapsed);
        PFunnelRegexShow();
        pFunnelConditions.ForEach(PFunnelConditionApply);
        PFunnelRelayUpdate();
    }

    private void PFunnelRegexShow()
    {
        pFunnelRegexField.Text = lFunnel.QFunnelCriterion.QFunnelRegexResolve(PFunnelRule, pFunnelRegexField.Text);
        pFunnelWholeBox.IsChecked = PFunnelRule.QFunnelRuleWhole;
    }

    private static void PFunnelConditionApply(PFunnelCondition pCondition) => pCondition.PFunnelConditionUpdate();

    private PFunnelCondition PFunnelConditionBuild(QFunnelCondition lCondition) =>
        new(lFunnel, PFunnelRule, lCondition)
        {
            Visibility = PLook.PLookVisible[PFunnelRule.QFunnelRuleFields]
        };

    private TextBox PFunnelRegexBuild()
    {
        TextBox pField = PFunnelRegexCreate();
        QTextbox.QTextboxAttach(pField);
        PFunnelRegexAttach(pField);
        return pField;
    }

    private static TextBox PFunnelRegexCreate() => new()
    {
        Height = PFunnelFieldHeight,
        FontSize = 12,
        FontFamily = pFunnelFontFamily,
        Margin = new Thickness(0, 0, 0, 8)
    };

    private void PFunnelRegexAttach(TextBox pField) =>
        pField.TextChanged += (_, _) => lFunnel.QFunnelCriterion.QFunnelRegexSet(PFunnelRule, pField.Text);

    private CheckBox PFunnelWholeBuild()
    {
        CheckBox pBox = PFunnelWholeCreate();
        QInteractionClick.QInteractionClickAttach(pBox);
        QCheckbox.QCheckboxAttach(pBox);
        PFunnelWholeAttach(pBox);
        return pBox;
    }

    private static CheckBox PFunnelWholeCreate() => new()
    {
        Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Whole"),
        FontSize = 12,
        FontFamily = pFunnelFontFamily,
        Foreground = pFunnelTitleBrush,
        Margin = new Thickness(2, 0, 0, 0)
    };

    private void PFunnelWholeAttach(CheckBox pBox)
    {
        pBox.Checked += (_, _) => lFunnel.QFunnelCriterion.QFunnelWholeSet(PFunnelRule, true);
        pBox.Unchecked += (_, _) => lFunnel.QFunnelCriterion.QFunnelWholeSet(PFunnelRule, false);
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

    private void PFunnelRelayUpdate()
    {
        PFunnelSelectionDetach();
        PFunnelRelayShow();
        PFunnelSelectionAttach();
    }

    private void PFunnelSelectionDetach() => pFunnelRelayCombo.SelectionChanged -= PFunnelRelayHandle;

    private void PFunnelSelectionAttach() => pFunnelRelayCombo.SelectionChanged += PFunnelRelayHandle;

    private void PFunnelRelayShow()
    {
        pFunnelRelayCombo.ItemsSource = PFunnelOptionsResolve();
        pFunnelRelayCombo.SelectedValue = PFunnelRule.QFunnelRuleTarget;
    }

    private List<PActionRelayOption> PFunnelOptionsResolve() =>
        lFunnel.QFunnelOptionsRead().Select(PFunnelOptionBuild).ToList();

    private static PActionRelayOption PFunnelOptionBuild(QFunnelTarget lTarget) =>
        new(lTarget.QFunnelTargetId, lTarget.QFunnelTargetTitle, QStripIcon.QStripIconFind(lTarget.QFunnelTargetKey));

    private void PFunnelRelayHandle(object pSender, SelectionChangedEventArgs pArgs) =>
        lFunnel.QFunnelTargetSelect(PFunnelRule, pFunnelRelayCombo.SelectedIndex);

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
}
