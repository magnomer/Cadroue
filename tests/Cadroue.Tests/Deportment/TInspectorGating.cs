using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorGating
{
    [Fact]
    public void Source_ZeroSize_ReadsAbsent()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        int notices = 0;
        TInterface.TInspectorAttach(inspector, () => notices++);

        TInterface.TInspectorSourceSet(inspector, 1920, 1080);
        TInterface.TInspectorSourceSet(inspector, 0, 1080);

        Assert.False(inspector.LInspectorSourcePresent);
        Assert.Equal(0, inspector.LInspectorSourceWidth);
        Assert.Equal(2, notices);
    }

    [Fact]
    public void Step_SameName_IsSilent()
    {
        LInspector inspector = TInterface.TInspectorCreate();
        int notices = 0;
        TInterface.TInspectorAttach(inspector, () => notices++);

        TInterface.TInspectorStepSet(inspector, "Crop");
        TInterface.TInspectorStepSet(inspector, "Crop");
        TInterface.TInspectorMinimizedSet(inspector, true);

        Assert.Equal(2, notices);
        Assert.True(inspector.LInspectorMinimized);
    }

    [Fact]
    public void Capable_Off_TurnsPreviewOff()
    {
        LGamma gamma = TInterface.TGammaCreate();

        TInterface.TGammaCapableSet(gamma, true, true);
        Assert.True(TInterface.TGammaPreviewRead(gamma));

        TInterface.TGammaCapableSet(gamma, false, true);

        Assert.False(gamma.LGammaCapable);
        Assert.False(TInterface.TGammaPreviewRead(gamma));
    }

    [Fact]
    public void ToneCapable_RepeatedValue_IsSilent()
    {
        LTone tone = TInterface.TToneCreate();
        int notices = 0;
        TInterface.TToneAttach(tone, () => notices++);

        TInterface.TToneCapableSet(tone, false);
        TInterface.TToneCapableSet(tone, false);

        Assert.Equal(1, notices);
        Assert.False(tone.LToneCapable);
    }

    [Fact]
    public void WhitebalanceCapable_Off_DisarmsPicker()
    {
        LWhitebalance whitebalance = TInterface.TWhitebalanceCreate();
        TInterface.TWhitebalanceCapableSet(whitebalance, true, true);
        TInterface.TWhitebalanceToolSet(whitebalance, true, Cadroue.Application.LNeutralTarget.LNeutralTargetWhite);
        Assert.True(TInterface.TWhitebalanceToolRead(whitebalance));

        TInterface.TWhitebalanceCapableSet(whitebalance, false, true);

        Assert.False(TInterface.TWhitebalanceToolRead(whitebalance));
        TInterface.TWhitebalanceToolSet(whitebalance, true, Cadroue.Application.LNeutralTarget.LNeutralTargetGrey);
        Assert.False(TInterface.TWhitebalanceToolRead(whitebalance));
    }
}
