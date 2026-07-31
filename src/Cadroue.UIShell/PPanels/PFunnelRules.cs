using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PControlBar;
using Cadroue.UIShell.PMainArea;

namespace Cadroue.UIShell.PPanels;

public sealed class PFunnelRules : PPanel
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));

    private readonly StackPanel pFunnelRowPanel;
    private readonly TextBlock pFunnelEmptyNotice;
    private readonly List<PFunnelRuleRow> pFunnelRows = new();
    private Func<IReadOnlyList<LCourierOption>> pFunnelOptionsRead = static () => Array.Empty<LCourierOption>();

    public PFunnelRules() : base("")
    {
        MinWidth = 300;

        pFunnelRowPanel = new StackPanel { Margin = new Thickness(12, 12, 12, 12) };

        pFunnelEmptyNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Funnel.Empty"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16),
            IsHitTestVisible = false
        };

        var pBody = new Grid();
        pBody.Children.Add(pFunnelEmptyNotice);
        pBody.Children.Add(pFunnelRowPanel);

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pHeader = PFunnelHeaderBuild();
        UIElement pActionBar = PFunnelActionBuild();
        DockPanel.SetDock(pHeader, Dock.Top);
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);

        Content = PPanelBorderBuild(pRoot);
        PFunnelEmptyUpdate();
    }

    public void PFunnelOptionsSet(Func<IReadOnlyList<LCourierOption>> pOptionsRead)
    {
        pFunnelOptionsRead = pOptionsRead;
    }

    public IReadOnlyList<PFunnelRuleRow> PFunnelRulesRead() => pFunnelRows;

    public void PFunnelRulesSeed(IReadOnlyList<LPreferenceFunnelRuleRecord> pRuleRecords)
    {
        foreach (LPreferenceFunnelRuleRecord pRecord in pRuleRecords)
        {
            PFunnelRuleRow pRow = PFunnelRuleAdd();
            pRow.PFunnelRowSeed(
                pRecord.LPreferenceFunnelStartsWith,
                pRecord.LPreferenceFunnelEndsWith,
                pRecord.LPreferenceFunnelAndMode,
                pRecord.LPreferenceFunnelTargetIndex);
        }
    }

    public void PFunnelTargetsResolve(IReadOnlyList<PTabRecord> pTabRecords)
    {
        foreach (PFunnelRuleRow pRow in pFunnelRows)
        {
            int pTargetIndex = pRow.PFunnelRowTargetPending;
            if (pTargetIndex >= 0 && pTargetIndex < pTabRecords.Count)
            {
                pRow.PFunnelRowTargetSet(pTabRecords[pTargetIndex].PTabId);
            }
            else
            {
                pRow.PFunnelRowTargetSet(Guid.Empty);
            }
        }
    }

    public PFunnelRuleRow PFunnelRuleAdd()
    {
        var pRow = new PFunnelRuleRow(pFunnelOptionsRead);
        pRow.PFunnelRowRemove += PFunnelRuleRemove;
        pFunnelRows.Add(pRow);
        pFunnelRowPanel.Children.Add(pRow);
        PFunnelEmptyUpdate();
        return pRow;
    }

    private void PFunnelRuleRemove(PFunnelRuleRow pRow)
    {
        pFunnelRows.Remove(pRow);
        pFunnelRowPanel.Children.Remove(pRow);
        PFunnelEmptyUpdate();
    }

    private void PFunnelEmptyUpdate()
    {
        pFunnelEmptyNotice.Visibility = pFunnelRows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static Border PFunnelHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Inspector.Funnel.Title"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pFunnelTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pTitleLabel
        };
    }

    private Border PFunnelActionBuild()
    {
        var pAddButton = new Button
        {
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = LLocalization.LLocalizationTextRead("Inspector.Funnel.Add"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            Foreground = pFunnelAccentBrush,
            Background = Brushes.White,
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            FocusVisualStyle = null,
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Funnel.Add")
        };
        pAddButton.Click += (_, _) => PFunnelRuleAdd();

        return new Border
        {
            Padding = new Thickness(12, 8, 12, 8),
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pAddButton
        };
    }
}
