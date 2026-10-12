using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PCabin;

internal sealed class PRosterQueue : ItemsControl
{
    private readonly ConditionalWeakTable<QRosterEntry, UIElement>.CreateValueCallback pRosterQueueSource;
    private readonly ConditionalWeakTable<QRosterEntry, UIElement> pRosterQueueBuilt = new();

    public PRosterQueue(Func<QRosterEntry, UIElement> pBuild)
    {
        pRosterQueueSource = lEntry => pBuild(lEntry);
        Focusable = false;
        ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
        VirtualizingPanel.SetIsVirtualizing(this, true);
        VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Recycling);
        VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Item);
        Template = PRosterTemplateBuild();
    }

    private static ControlTemplate PRosterTemplateBuild()
    {
        var pScroll = new FrameworkElementFactory(typeof(ScrollViewer));
        pScroll.SetValue(ScrollViewer.CanContentScrollProperty, true);
        pScroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        pScroll.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        pScroll.SetValue(FocusVisualStyleProperty, null);
        pScroll.SetValue(Control.PaddingProperty, new Thickness(8));
        pScroll.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter)));
        return new ControlTemplate(typeof(PRosterQueue)) { VisualTree = pScroll };
    }

    protected override bool IsItemItsOwnContainerOverride(object pItem) => false;

    protected override DependencyObject GetContainerForItemOverride() => new ContentControl { Focusable = false };

    protected override void PrepareContainerForItemOverride(DependencyObject pElement, object pItem) =>
        ((ContentControl)pElement).Content = pRosterQueueBuilt.GetValue((QRosterEntry)pItem, pRosterQueueSource);

    protected override void ClearContainerForItemOverride(DependencyObject pElement, object pItem) =>
        ((ContentControl)pElement).Content = null;

    protected override AutomationPeer OnCreateAutomationPeer() => new PRosterQueuePeer(this);
}
