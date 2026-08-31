using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TPreviewGeometry
{
    [Fact]
    public void Apply_Rotate270_ResolvesRotation()
    {
        LPreviewState state = TInterface.TPreviewRotateChange(
            TInterface.TPreviewDefaultCreate(),
            TInterface.TRotateFlipCreate(LRotateKind.LRotate270, false, false));

        var result = new TPreview().TPreviewApply(state);

        Assert.Equal(270u, result.TPreviewRotation);
    }
}
