using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainArea;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public enum PFunnelForm { Filename, Regex, Remainder }

public sealed class PFunnelRuleRow : Border
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));

    private static readonly (PFunnelKind Kind, string LabelKey, bool HasJoin)[] pFunnelSpecs =
    {
        (PFunnelKind.Contains, "Inspector.Funnel.Contains", false),
        (PFunnelKind.Start, "Inspector.Funnel.StartsWith", true),
        (PFunnelKind.End, "Inspector.Funnel.EndsWith", true),
        (PFunnelKind.Extension, "Inspector.Funnel.Extension", true)
    };

    private const double PFunnelFieldHeight = 30;

    private readonly PFunnelForm pFunnelForm;
    private readonly List<PFunnelCondition> pFunnelConditions = new();
    private readonly ComboBox pFunnelRelayCombo;
    private readonly Func<IReadOnlyList<PActionRelayOption>> pFunnelOptionsRead;
    private readonly PFunnelRuleFrame pFunnelFrame;
    private TextBox? pFunnelRegexField;
    private CheckBox? pFunnelWholeCheck;

    private bool pFunnelRelayBusy;
    private Guid pFunnelTargetId;
    private int pFunnelTargetPending = -1;

    public event Action? PFunnelRowChange;
    public event Action<PFunnelRuleRow>? PFunnelRowRemove;

    public PFunnelRuleRow(Func<IReadOnlyList<PActionRelayOption>> pOptionsRead, PFunnelForm pForm = PFunnelForm.Filename)
    {
        pFunnelForm = pForm;
        pFunnelOptionsRead = pOptionsRead;
        pFunnelRelayCombo = PFunnelRelayBuild();

        var pBody = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
        if (pForm == PFunnelForm.Regex)
        {
            pBody.Children.Add(PFunnelRegexBuild());
        }
        else if (pForm == PFunnelForm.Filename)
        {
            foreach ((PFunnelKind pKind, string pLabelKey, bool pHasJoin) in pFunnelSpecs)
            {
                var pCondition = new PFunnelCondition(pKind, pLabelKey, pHasJoin);
                pCondition.PFunnelConditionChange += () => PFunnelRowChange?.Invoke();
                pFunnelConditions.Add(pCondition);
                pBody.Children.Add(pCondition);
            }
        }

        pBody.Children.Add(PFunnelTargetBuild());

        string pTitleKey = pForm switch
        {
            PFunnelForm.Regex => "Inspector.Funnel.Regex",
            PFunnelForm.Remainder => "Inspector.Funnel.Remainder",
            _ => "Inspector.Funnel.Filename"
        };
        pFunnelFrame = new PFunnelRuleFrame(pBody, pTitleKey, () => PFunnelRowRemove?.Invoke(this));

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

        PFunnelRelayRebuild();
    }

    public Border PFunnelHeader => pFunnelFrame.PFunnelHeader;

    public bool PFunnelRemainder => pFunnelForm == PFunnelForm.Remainder;

    public Guid PFunnelTargetId => pFunnelTargetId;

    public int PFunnelTargetPending => pFunnelTargetPending;

    public void PFunnelOrderSet(int pOrder) => pFunnelFrame.PFunnelOrderSet(pOrder);

    public void PFunnelSelectSet(bool pSelected) => pFunnelFrame.PFunnelSelectSet(pSelected);

    public void PFunnelTargetSet(Guid pTargetId)
    {
        pFunnelTargetId = pTargetId;
        pFunnelTargetPending = -1;
        PFunnelRelayRebuild();
    }

    public void PFunnelRowRestore(LSceneFunnelRule pRecord)
    {
        if (pFunnelForm == PFunnelForm.Remainder)
        {
            pFunnelTargetPending = pRecord.LSceneFunnelTarget;
            return;
        }

        if (pFunnelForm == PFunnelForm.Regex)
        {
            if (pFunnelRegexField is not null)
            {
                pFunnelRegexField.Text = pRecord.LSceneFunnelRegex;
            }

            if (pFunnelWholeCheck is not null)
            {
                pFunnelWholeCheck.IsChecked = pRecord.LSceneFunnelWhole;
            }
        }
        else
        {
            PFunnelConditionFind(PFunnelKind.Contains).PFunnelConditionRestore(pRecord.LSceneFunnelContains);
            PFunnelConditionFind(PFunnelKind.Start).PFunnelConditionRestore(pRecord.LSceneFunnelPrefix);
            PFunnelConditionFind(PFunnelKind.End).PFunnelConditionRestore(pRecord.LSceneFunnelEnd);
            PFunnelConditionFind(PFunnelKind.Extension).PFunnelConditionRestore(pRecord.LSceneFunnelExtension);
        }

        pFunnelTargetPending = pRecord.LSceneFunnelTarget;
    }

    public LSceneFunnelRule PFunnelRecordCreate()
    {
        if (pFunnelForm == PFunnelForm.Remainder)
        {
            return new LSceneFunnelRule
            {
                LSceneFunnelType = (int)PFunnelForm.Filename,
                LSceneFunnelRemainder = true
            };
        }

        if (pFunnelForm == PFunnelForm.Regex)
        {
            return new LSceneFunnelRule
            {
                LSceneFunnelType = (int)PFunnelForm.Regex,
                LSceneFunnelRegex = pFunnelRegexField?.Text.Trim() ?? string.Empty,
                LSceneFunnelWhole = pFunnelWholeCheck?.IsChecked == true
            };
        }

        return new LSceneFunnelRule
        {
            LSceneFunnelType = (int)PFunnelForm.Filename,
            LSceneFunnelContains = PFunnelConditionFind(PFunnelKind.Contains).PFunnelConditionRead(),
            LSceneFunnelPrefix = PFunnelConditionFind(PFunnelKind.Start).PFunnelConditionRead(),
            LSceneFunnelEnd = PFunnelConditionFind(PFunnelKind.End).PFunnelConditionRead(),
            LSceneFunnelExtension = PFunnelConditionFind(PFunnelKind.Extension).PFunnelConditionRead()
        };
    }

    private PFunnelCondition PFunnelConditionFind(PFunnelKind pKind)
        => pFunnelConditions.First(pItem => pItem.PFunnelConditionKind == pKind);

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
        pFunnelRegexField.TextChanged += (_, _) => PFunnelRowChange?.Invoke();

        pFunnelWholeCheck = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Funnel.Whole"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelTitleBrush,
            Margin = new Thickness(2, 0, 0, 0)
        };
        PCheckbox.PCheckboxApply(pFunnelWholeCheck);
        pFunnelWholeCheck.Checked += (_, _) => PFunnelRowChange?.Invoke();
        pFunnelWholeCheck.Unchecked += (_, _) => PFunnelRowChange?.Invoke();

        pStack.Children.Add(pFunnelRegexField);
        pStack.Children.Add(pFunnelWholeCheck);
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
        pCombo.DropDownOpened += (_, _) => PFunnelRelayRebuild();
        pCombo.SelectionChanged += PFunnelRelayHandle;
        return pCombo;
    }

    private void PFunnelRelayRebuild()
    {
        pFunnelRelayBusy = true;
        var pOptions = new List<PActionRelayOption>
        {
            new(Guid.Empty, LLocalization.LLocalizationTextRead("Inspector.Funnel.RelayNone"), null)
        };
        pOptions.AddRange(pFunnelOptionsRead());
        if (pFunnelTargetId != Guid.Empty && pOptions.All(pOption => pOption.PActionRelayId != pFunnelTargetId))
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
