using Cadroue.Application;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Cadroue.UIVeneer.PToolbar;
using Cadroue.UIVeneer.PDeck;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed class PFunnelRuleRow : Border
{
    private readonly record struct PFunnelSpec(
        LFunnelKind PFunnelSpecKind,
        string PFunnelSpecLabel,
        bool PFunnelSpecJoin);

    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    private static readonly PFunnelSpec[] pFunnelSpecs =
    {
        new(LFunnelKind.LFunnelKindContains, "Inspector.Funnel.Contains", false),
        new(LFunnelKind.LFunnelKindPrefix, "Inspector.Funnel.StartsWith", true),
        new(LFunnelKind.LFunnelKindEnd, "Inspector.Funnel.EndsWith", true),
        new(LFunnelKind.LFunnelKindExtension, "Inspector.Funnel.Extension", true)
    };

    private const double PFunnelFieldHeight = 30;

    private readonly LFunnel lFunnel;
    private readonly List<PFunnelCondition> pFunnelConditions = new();
    private readonly ComboBox pFunnelRelayCombo;
    private readonly Func<IReadOnlyList<PActionRelayOption>> pFunnelOptionsSource;
    private readonly PFunnelRuleFrame pFunnelFrame;
    private TextBox? pFunnelRegexField;
    private CheckBox? pFunnelWholeBox;

    public PFunnelRuleRow(
        LFunnel lFunnelOwner,
        LFunnelRule lFunnelRule,
        Func<IReadOnlyList<PActionRelayOption>> pOptionsRead)
    {
        lFunnel = lFunnelOwner;
        PFunnelRule = lFunnelRule;
        pFunnelOptionsSource = pOptionsRead;
        pFunnelRelayCombo = PFunnelRelayBuild();

        var pBody = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
        if (lFunnelRule.LFunnelRuleForm == LFunnelForm.LFunnelFormRegex)
        {
            pBody.Children.Add(PFunnelRegexBuild());
        }
        else if (lFunnelRule.LFunnelRuleForm == LFunnelForm.LFunnelFormFilename)
        {
            foreach ((LFunnelKind pKind, string pLabelKey, bool pHasJoin) in pFunnelSpecs)
            {
                var pCondition = new PFunnelCondition(lFunnelOwner, lFunnelRule, pKind, pLabelKey, pHasJoin);
                pFunnelConditions.Add(pCondition);
                pBody.Children.Add(pCondition);
            }
        }

        pBody.Children.Add(PFunnelTargetBuild());

        string pTitleKey = lFunnelRule.LFunnelRuleForm switch
        {
            LFunnelForm.LFunnelFormRegex => "Inspector.Funnel.Regex",
            LFunnelForm.LFunnelFormRemainder => "Inspector.Funnel.Remainder",
            _ => "Inspector.Funnel.Filename"
        };
        pFunnelFrame = new PFunnelRuleFrame(
            pBody,
            pTitleKey,
            () => lFunnelOwner.LFunnelRuleRemove(lFunnelRule),
            () => lFunnelOwner.LFunnelCollapsedSet(lFunnelRule, !lFunnelRule.LFunnelRuleCollapsed));

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
        if (pFunnelRegexField is not null && pFunnelRegexField.Text.Trim() != PFunnelRule.LFunnelRuleRegex)
        {
            pFunnelRegexField.Text = PFunnelRule.LFunnelRuleRegex;
        }

        if (pFunnelWholeBox is not null)
        {
            pFunnelWholeBox.IsChecked = PFunnelRule.LFunnelRuleWhole;
        }

        foreach (PFunnelCondition pCondition in pFunnelConditions)
        {
            pCondition.PFunnelConditionUpdate();
        }

        PFunnelRelayRebuild(false);
    }

    private UIElement PFunnelRegexBuild()
    {
        var pStack = new StackPanel();

        pFunnelRegexField = new TextBox
        {
            Height = PFunnelFieldHeight,
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Margin = new Thickness(0, 0, 0, 8)
        };
        PTextbox.PTextboxApply(pFunnelRegexField);
        pFunnelRegexField.TextChanged += (_, _) => lFunnel.LFunnelRegexSet(PFunnelRule, pFunnelRegexField.Text);

        pFunnelWholeBox = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Funnel.Whole"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelTitleBrush,
            Margin = new Thickness(2, 0, 0, 0)
        };
        PCheckbox.PCheckboxApply(pFunnelWholeBox);
        pFunnelWholeBox.Checked += (_, _) => lFunnel.LFunnelWholeSet(PFunnelRule, true);
        pFunnelWholeBox.Unchecked += (_, _) => lFunnel.LFunnelWholeSet(PFunnelRule, false);

        pStack.Children.Add(pFunnelRegexField);
        pStack.Children.Add(pFunnelWholeBox);
        return pStack;
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
        pCombo.DropDownOpened += (_, _) => PFunnelRelayRebuild(true);
        pCombo.SelectionChanged += PFunnelRelayHandle;
        return pCombo;
    }

    private void PFunnelRelayRebuild(bool pForce)
    {
        if (!pForce
            && pFunnelRelayCombo.ItemsSource is not null
            && pFunnelRelayCombo.SelectedValue is Guid pShown
            && pShown == PFunnelRule.LFunnelRuleTarget)
        {
            return;
        }

        var pOptions = new List<PActionRelayOption>
        {
            new(Guid.Empty, LLocalization.LLocalizationTextRead("Inspector.Funnel.RelayNone"), null)
        };
        pOptions.AddRange(pFunnelOptionsSource());
        Guid pTargetId = PFunnelRule.LFunnelRuleTarget;
        if (pTargetId != Guid.Empty && pOptions.All(pOption => pOption.PActionRelayId != pTargetId))
        {
            lFunnel.LFunnelTargetSet(PFunnelRule, Guid.Empty);
            pTargetId = Guid.Empty;
        }

        pFunnelRelayCombo.SelectionChanged -= PFunnelRelayHandle;
        pFunnelRelayCombo.ItemsSource = pOptions;
        pFunnelRelayCombo.SelectedValue = pTargetId;
        pFunnelRelayCombo.SelectionChanged += PFunnelRelayHandle;
    }

    private void PFunnelRelayHandle(object pSender, SelectionChangedEventArgs pArgs)
    {
        if (pFunnelRelayCombo.SelectedValue is Guid pTargetId)
        {
            lFunnel.LFunnelTargetSet(PFunnelRule, pTargetId);
        }
    }

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
