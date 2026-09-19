using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TDividerDrag
{
    [Fact]
    public void Move_GrowsWhenDraggedUp_AndClampsToTheBounds()
    {
        LDivider divider = TInterface.TDividerCreate();
        Assert.False(divider.LDividerActive);

        TInterface.TDividerPressHandle(divider, 500, 300);
        Assert.True(divider.LDividerActive);
        Assert.Equal(340, TInterface.TDividerMoveResolve(divider, 460, 300));
        Assert.Equal(260, TInterface.TDividerMoveResolve(divider, 540, 340));
        Assert.Equal(LDivider.LDividerMaximum, TInterface.TDividerMoveResolve(divider, 0, 300));
        Assert.Equal(LDivider.LDividerMinimum, TInterface.TDividerMoveResolve(divider, 900, 300));

        TInterface.TDividerRelease(divider);
        Assert.False(divider.LDividerActive);
        Assert.Equal(300, TInterface.TDividerMoveResolve(divider, 460, 300));
    }
}
