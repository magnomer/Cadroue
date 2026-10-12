using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TDividerDrag
{
    [Fact]
    public void Move_GrowsWhenDraggedUp_AndClampsToTheBounds()
    {
        QDivider divider = TInterface.TDividerCreate();
        Assert.False(divider.QDividerActive);

        TInterface.TDividerPressHandle(divider, 500, 300);
        Assert.True(divider.QDividerActive);
        Assert.Equal(340, TInterface.TDividerMoveResolve(divider, 460, 300));
        Assert.Equal(260, TInterface.TDividerMoveResolve(divider, 540, 340));
        Assert.Equal(QDivider.QDividerMaximum, TInterface.TDividerMoveResolve(divider, 0, 300));
        Assert.Equal(QDivider.QDividerMinimum, TInterface.TDividerMoveResolve(divider, 900, 300));

        TInterface.TDividerRelease(divider);
        Assert.False(divider.QDividerActive);
        Assert.Equal(300, TInterface.TDividerMoveResolve(divider, 460, 300));
    }
}
