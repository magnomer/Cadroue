using Cadroue.Application;

namespace Cadroue.Tests;

internal static class TNeutral
{
    internal static LNeutralSample Resolve(
        byte[] pixels, int width, int height, int centerX, int centerY) =>
        LNeutral.LNeutralResolve(pixels, width, height, centerX, centerY);
}
