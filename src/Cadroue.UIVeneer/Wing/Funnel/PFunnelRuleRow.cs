using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

public sealed class PFunnelRuleRow : Border
{
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));

    private readonly QFunnel lFunnel;
    private readonly List<PFunnelCondition> pFunnelConditions;
    private readonly PFunnelRuleRelay pFunnelRelay;
    private readonly PFunnelRuleFrame pFunnelFrame;
    private readonly PFunnelRuleRegex pFunnelRegex;

    public PFunnelRuleRow(QFunnel lFunnelOwner, QFunnelRule lFunnelRule)
    {
        lFunnel = lFunnelOwner;
        PFunnelRule = lFunnelRule;
        pFunnelRelay = new PFunnelRuleRelay(lFunnelOwner.QFunnelRelay, lFunnelRule);
        pFunnelRegex = new PFunnelRuleRegex(lFunnelOwner.QFunnelCriterion, lFunnelRule);
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

    private StackPanel PFunnelBodyBuild()
    {
        var pBody = new StackPanel { Margin = new Thickness(10, 8, 10, 10) };
        pBody.Children.Add(pFunnelRegex.PFunnelRegexStack);
        pFunnelConditions.ForEach(pCondition => pBody.Children.Add(pCondition));
        pBody.Children.Add(pFunnelRelay.PFunnelTargetBuild());
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
        pFunnelRegex.PFunnelRegexShow();
        pFunnelConditions.ForEach(PFunnelConditionApply);
        pFunnelRelay.PFunnelRelayUpdate();
    }

    private static void PFunnelConditionApply(PFunnelCondition pCondition) => pCondition.PFunnelConditionUpdate();

    private PFunnelCondition PFunnelConditionBuild(QFunnelCondition lCondition) =>
        new(lFunnel, PFunnelRule, lCondition)
        {
            Visibility = PLook.PLookVisible[PFunnelRule.QFunnelRuleFields]
        };
}
