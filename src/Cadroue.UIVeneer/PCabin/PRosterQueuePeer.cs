using System.Windows.Automation.Peers;

namespace Cadroue.UIVeneer.PCabin;

internal sealed class PRosterQueuePeer : FrameworkElementAutomationPeer
{
    public PRosterQueuePeer(PRosterQueue pOwner)
        : base(pOwner)
    {
    }

    protected override List<AutomationPeer>? GetChildrenCore() => null;

    protected override string GetClassNameCore() => nameof(PRosterQueue);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
}
