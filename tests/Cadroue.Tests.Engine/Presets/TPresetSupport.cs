using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

[Collection("Preset")]
public sealed class TPresetSupport
{
    [Fact]
    public void SelectionSupported_EditKind_RefusesCopyVideo_KindlessAcceptsAny()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Encode", "Copy");

        Assert.True(presets.TPresetSupportedRead("Encode", "Edit"));
        Assert.False(presets.TPresetSupportedRead("Copy", "Edit"));
        Assert.True(presets.TPresetSupportedRead("Copy", string.Empty));
    }
}
