using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TInspectorGating
{
    [Fact]
    public void Source_ZeroSize_ReadsAbsent()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        TInterfaceInspector.TInspectorAttach(inspector, () => notices++);

        TInterfaceInspector.TInspectorSourceSet(inspector, 1920, 1080);
        TInterfaceInspector.TInspectorSourceSet(inspector, 0, 1080);

        Assert.False(inspector.QInspectorSourcePresent);
        Assert.Equal(0, inspector.QInspectorSourceWidth);
        Assert.Equal(2, notices);
    }

    [Fact]
    public void Step_SameName_IsSilent()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();
        int notices = 0;
        TInterfaceInspector.TInspectorAttach(inspector, () => notices++);

        TInterfaceInspector.TInspectorStepSet(inspector, "Crop");
        TInterfaceInspector.TInspectorStepSet(inspector, "Crop");
        TInterfaceInspector.TInspectorMinimizedSet(inspector, true);

        Assert.Equal(2, notices);
        Assert.True(inspector.QInspectorMinimized);
    }

    [Fact]
    public void Capable_Off_TurnsPreviewOff()
    {
        CGamma gamma = TInterface.TGammaCreate();

        TInterface.TGammaCapableSet(gamma, true, true);
        Assert.True(TInterface.TGammaPreviewRead(gamma));

        TInterface.TGammaCapableSet(gamma, false, true);

        Assert.False(gamma.CGammaCapable);
        Assert.False(TInterface.TGammaPreviewRead(gamma));
    }

    [Fact]
    public void ToneCapable_RepeatedValue_IsSilent()
    {
        QTone tone = TInterface.TToneCreate();
        int notices = 0;
        TInterface.TToneAttach(tone, () => notices++);

        TInterface.TToneCapableSet(tone, false);
        TInterface.TToneCapableSet(tone, false);

        Assert.Equal(1, notices);
        Assert.False(tone.QToneCapable);
    }

    [Fact]
    public void WhitebalanceCapable_Off_DisarmsPicker()
    {
        QWhitebalance whitebalance = TInterface.TWhitebalanceCreate();
        TInterface.TWhitebalanceCapableSet(whitebalance, true, true);
        TInterface.TWhitebalanceToolSet(whitebalance, true, Cadroue.Application.LNeutralTarget.LNeutralTargetWhite);
        Assert.True(TInterface.TWhitebalanceToolRead(whitebalance));

        TInterface.TWhitebalanceCapableSet(whitebalance, false, true);

        Assert.False(TInterface.TWhitebalanceToolRead(whitebalance));
        TInterface.TWhitebalanceToolSet(whitebalance, true, Cadroue.Application.LNeutralTarget.LNeutralTargetGrey);
        Assert.False(TInterface.TWhitebalanceToolRead(whitebalance));
    }

    [Fact]
    public void Owner_FirstPath_ReadsFirst()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();

        Assert.True(TInterfaceInspector.TInspectorOwnerSet(inspector, "a.mp4"));
        Assert.False(TInterfaceInspector.TInspectorOwnerSet(inspector, "b.mp4"));
        Assert.Equal("b.mp4", inspector.QInspectorOwnerPath);
    }

    [Fact]
    public void Failure_SamePath_RecordsOnce()
    {
        QInspector inspector = TInterfaceInspector.TInspectorCreate();

        Assert.True(TInterfaceInspector.TInspectorFailureSet(inspector, "a.mp4"));
        Assert.False(TInterfaceInspector.TInspectorFailureSet(inspector, "A.MP4"));
        Assert.False(TInterfaceInspector.TInspectorFailureSet(inspector, null));
        Assert.True(TInterfaceInspector.TInspectorFailureSet(inspector, "a.mp4"));
    }
}
