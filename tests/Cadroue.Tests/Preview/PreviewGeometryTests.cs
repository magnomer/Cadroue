using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class PreviewGeometryTests
{
    [Fact]
    public void Apply_Rotate270_ResolvesRotation()
    {
        LPreviewState state = TInterface.PreviewRotateFlipChange(
            TInterface.PreviewDefaultCreate(),
            TInterface.RotateFlipCreate(LRotateKind.LRotate270, false, false));

        var result = new TPreview().ApplyState(state);

        Assert.Equal(270u, result.Rotation);
    }
}
