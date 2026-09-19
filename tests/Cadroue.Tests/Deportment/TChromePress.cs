using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TChromePress
{
    [Fact]
    public void Press_OnCaption_Arms_AndDragStartsPastThreshold()
    {
        LChrome chrome = TInterface.TChromeCreate();

        TInterface.TChromePressHandle(chrome, true, 100, 10);
        Assert.True(chrome.LChromeArmed);
        Assert.False(chrome.LChromeDragActive);

        Assert.False(TInterface.TChromeMoveResolve(chrome, 102, 11, true, 4, 4));
        Assert.False(TInterface.TChromeMoveResolve(chrome, 120, 10, false, 4, 4));
        Assert.True(TInterface.TChromeMoveResolve(chrome, 120, 10, true, 4, 4));
        Assert.True(chrome.LChromeDragActive);
        Assert.False(TInterface.TChromeMoveResolve(chrome, 130, 10, true, 4, 4));

        TInterface.TChromeReset(chrome);
        Assert.False(chrome.LChromeArmed);
        Assert.False(chrome.LChromeDragActive);
    }

    [Fact]
    public void Press_OffCaption_NeverArms()
    {
        LChrome chrome = TInterface.TChromeCreate();

        TInterface.TChromePressHandle(chrome, false, 100, 10);
        Assert.False(chrome.LChromeArmed);
        Assert.False(TInterface.TChromeMoveResolve(chrome, 200, 10, true, 4, 4));
        Assert.False(TInterface.TChromeReleaseResolve(chrome, true));
    }

    [Fact]
    public void Release_IsAClick_OnlyWhenArmedWithoutDrag_AndOnLogo()
    {
        LChrome chrome = TInterface.TChromeCreate();

        TInterface.TChromePressHandle(chrome, true, 0, 0);
        Assert.False(TInterface.TChromeReleaseResolve(chrome, false));
        Assert.False(chrome.LChromeArmed);

        TInterface.TChromePressHandle(chrome, true, 0, 0);
        Assert.True(TInterface.TChromeReleaseResolve(chrome, true));

        TInterface.TChromePressHandle(chrome, true, 0, 0);
        Assert.True(TInterface.TChromeMoveResolve(chrome, 50, 0, true, 4, 4));
        Assert.False(TInterface.TChromeReleaseResolve(chrome, true));
    }

    [Fact]
    public void CaptionAndDouble_AreBareFacts()
    {
        Assert.True(TInterface.TChromeCaptionResolve(false, false, true));
        Assert.False(TInterface.TChromeCaptionResolve(true, false, true));
        Assert.False(TInterface.TChromeCaptionResolve(false, true, true));
        Assert.False(TInterface.TChromeCaptionResolve(false, false, false));
        Assert.True(TInterface.TChromeDoubleCheck(2, true));
        Assert.False(TInterface.TChromeDoubleCheck(2, false));
        Assert.False(TInterface.TChromeDoubleCheck(1, true));
    }
}
