using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PPorch;

namespace Cadroue.UIVeneer.PWing;

public sealed class PFunnelRules : UserControl
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));

    private readonly QFunnel lFunnel;
    private readonly Grid pFunnelRowGrid;
    private readonly TextBlock pFunnelEmptyNotice;
    private readonly Dictionary<QFunnelRule, PFunnelRuleRow> pFunnelRows = new();
    private readonly PFunnelRulesDrag pFunnelDrag;
    private readonly PFunnelRulesAction pFunnelAction;

    public PFunnelRules(QFunnel lFunnelOwner)
    {
        QPanel.QPanelAttach(this);
        lFunnel = lFunnelOwner;
        PFunnelWidthApply();
        pFunnelRowGrid = PFunnelGridBuild();
        pFunnelEmptyNotice = PFunnelNoticeBuild();
        pFunnelDrag = new PFunnelRulesDrag(lFunnelOwner, pFunnelRowGrid, pFunnelRows);
        pFunnelAction = new PFunnelRulesAction(lFunnelOwner);
        PFunnelContentBuild();
        PFunnelHandlerAttach();
        lFunnel.QFunnelRules.ToList().ForEach(PFunnelRowAdd);
        PFunnelRowsUpdate();
    }

    private void PFunnelWidthApply() => MinWidth = 300;

    private static Grid PFunnelGridBuild() => new() { Margin = new Thickness(12, 12, 12, 12) };

    private static TextBlock PFunnelNoticeBuild() => new()
    {
        Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Empty"),
        FontSize = 12,
        FontFamily = pFunnelFontFamily,
        Foreground = pFunnelMutedBrush,
        TextWrapping = TextWrapping.Wrap,
        HorizontalAlignment = HorizontalAlignment.Center,
        TextAlignment = TextAlignment.Center,
        Margin = new Thickness(16, 24, 16, 16),
        IsHitTestVisible = false
    };

    private void PFunnelContentBuild()
    {
        var pBody = new Grid();
        pBody.Children.Add(pFunnelEmptyNotice);
        pBody.Children.Add(pFunnelRowGrid);

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pHeader = PFunnelHeaderBuild();
        UIElement pActionBar = pFunnelAction.PFunnelActionBuild();
        DockPanel.SetDock(pHeader, Dock.Top);
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);

        Content = QPanel.QPanelBuild(pRoot);
    }

    private void PFunnelHandlerAttach()
    {
        lFunnel.QFunnelRuleCreated += PFunnelRowAdd;
        lFunnel.QFunnelRuleDeleted += PFunnelRowRemove;
        lFunnel.QFunnelChanged += PFunnelRowsUpdate;
        lFunnel.QFunnelRuleChanged += PFunnelRuleHandle;
    }

    private void PFunnelRowAdd(QFunnelRule lRule)
    {
        var pRow = new PFunnelRuleRow(lFunnel, lRule);
        PFunnelRowAttach(pRow, lRule);
        PFunnelMapAdd(lRule, pRow);
        PFunnelGridAppend(pRow);
    }

    private void PFunnelRowAttach(PFunnelRuleRow pRow, QFunnelRule lRule)
    {
        pFunnelDrag.PFunnelDragAttach(pRow);
        pRow.PreviewMouseLeftButtonDown += (_, _) => lFunnel.QFunnelRuleSelect(lRule);
        pRow.GotKeyboardFocus += (_, _) => lFunnel.QFunnelRuleSelect(lRule);
    }

    private void PFunnelMapAdd(QFunnelRule lRule, PFunnelRuleRow pRow) => pFunnelRows.Add(lRule, pRow);

    private void PFunnelGridAppend(PFunnelRuleRow pRow)
    {
        pFunnelRowGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pFunnelRowGrid.Children.Add(pRow);
    }

    private void PFunnelRowRemove(QFunnelRule lRule)
    {
        PFunnelGridRemove(pFunnelRows[lRule]);
        PFunnelMapRemove(lRule);
    }

    private void PFunnelGridRemove(PFunnelRuleRow pRow) => pFunnelRowGrid.Children.Remove(pRow);

    private void PFunnelMapRemove(QFunnelRule lRule) => pFunnelRows.Remove(lRule);

    private void PFunnelRuleHandle(QFunnelRule lRule) => pFunnelRows[lRule].PFunnelRowUpdate();

    private void PFunnelRowsUpdate()
    {
        lFunnel.QFunnelSlotsRead().ToList().ForEach(PFunnelSlotApply);
        PFunnelEmptyApply();
    }

    private void PFunnelEmptyApply() => pFunnelEmptyNotice.Visibility = PLook.PLookVisible[lFunnel.QFunnelEmpty];

    private void PFunnelSlotApply(QFunnelSlot lSlot)
    {
        PFunnelRuleRow pRow = pFunnelRows[lSlot.QFunnelSlotRule];
        Grid.SetRow(pRow, lSlot.QFunnelSlotIndex);
        pRow.PFunnelOrderSet(lSlot.QFunnelSlotOrder);
        pRow.PFunnelSelectSet(lSlot.QFunnelSlotSelected);
    }

    private static Border PFunnelHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Title"),
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pFunnelTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            MinHeight = 36,
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pTitleLabel
        };
    }
}
