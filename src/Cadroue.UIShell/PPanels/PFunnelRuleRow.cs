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

    private static readonly (PFunnelKind Kind, string LabelKey, bool HasJoin)[] pFunnelSpecs =
    {
        (PFunnelKind.Contains, "Inspector.Funnel.Contains", false),
        (PFunnelKind.Start, "Inspector.Funnel.StartsWith", true),
        (PFunnelKind.End, "Inspector.Funnel.EndsWith", true),
        (PFunnelKind.Extension, "Inspector.Funnel.Extension", true)
    };

    private const double PFunnelFieldHeight = 30;

    private readonly List<PFunnelCondition> pFunnelConditions = new();
    private readonly ComboBox pFunnelRelayCombo;
    private readonly Func<IReadOnlyList<LCourierOption>> pFunnelOptionsRead;
    private readonly PFunnelRuleFrame pFunnelFrame;

    private bool pFunnelRelayBusy;
    private Guid pFunnelTargetId;
    private int pFunnelTargetPending = -1;

    public event Action? PFunnelRowChange;
    public event Action<PFunnelRuleRow>? PFunnelRowRemove;

    public PFunnelRuleRow(Func<IReadOnlyList<LCourierOption>> pOptionsRead)
    {
        pFunnelOptionsRead = pOptionsRead;
        pFunnelRelayCombo = PFunnelRelayBuild();

        var pBody = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
        foreach ((PFunnelKind pKind, string pLabelKey, bool pHasJoin) in pFunnelSpecs)
        {
            var pCondition = new PFunnelCondition(pKind, pLabelKey, pHasJoin);
            pCondition.PFunnelConditionChange += () => PFunnelRowChange?.Invoke();
            pFunnelConditions.Add(pCondition);
            pBody.Children.Add(pCondition);
        }

        pBody.Children.Add(PFunnelTargetBuild());

        pFunnelFrame = new PFunnelRuleFrame(pBody, () => PFunnelRowRemove?.Invoke(this));

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

    public void PFunnelRowRestore(LPreferenceFunnelRuleRecord pRecord)
    {
        PFunnelConditionFind(PFunnelKind.Contains).PFunnelConditionRestore(pRecord.LPreferenceFunnelContains);
        PFunnelConditionFind(PFunnelKind.Start).PFunnelConditionRestore(pRecord.LPreferenceFunnelStart);
        PFunnelConditionFind(PFunnelKind.End).PFunnelConditionRestore(pRecord.LPreferenceFunnelEnd);
        PFunnelConditionFind(PFunnelKind.Extension).PFunnelConditionRestore(pRecord.LPreferenceFunnelExtension);
        pFunnelTargetPending = pRecord.LPreferenceFunnelTarget;
    }

    public LPreferenceFunnelRuleRecord PFunnelRecordCreate()
    {
        return new LPreferenceFunnelRuleRecord
        {
            LPreferenceFunnelContains = PFunnelConditionFind(PFunnelKind.Contains).PFunnelConditionRecordRead(),
            LPreferenceFunnelStart = PFunnelConditionFind(PFunnelKind.Start).PFunnelConditionRecordRead(),
            LPreferenceFunnelEnd = PFunnelConditionFind(PFunnelKind.End).PFunnelConditionRecordRead(),
            LPreferenceFunnelExtension = PFunnelConditionFind(PFunnelKind.Extension).PFunnelConditionRecordRead()
        };
    }

    public bool PFunnelRowMatch(string pFileName)
    {
        bool pHasResult = false;
        bool pAccumulator = false;

        foreach (PFunnelCondition pCondition in pFunnelConditions)
        {
            if (pCondition.PFunnelConditionText.Length == 0)
            {
                continue;
            }

            bool pResult = pCondition.PFunnelConditionMatch(pFileName);

            if (!pHasResult)
            {
                pAccumulator = pResult;
                pHasResult = true;
            }
            else
            {
                pAccumulator = pCondition.PFunnelConditionAnd
                    ? pAccumulator && pResult
                    : pAccumulator || pResult;
            }
        }

        return pHasResult && pAccumulator;
    }

    private PFunnelCondition PFunnelConditionFind(PFunnelKind pKind)
        => pFunnelConditions.First(pItem => pItem.PFunnelConditionKind == pKind);

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
