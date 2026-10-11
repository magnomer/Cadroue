using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

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
