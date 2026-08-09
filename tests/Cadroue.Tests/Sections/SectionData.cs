using Cadroue.Core;

namespace Cadroue.Tests;

internal static class SectionData
{
    internal static LPiece Seg(double startSeconds, double endSeconds) =>
        new(TimeSpan.FromSeconds(startSeconds), TimeSpan.FromSeconds(endSeconds), 0, string.Empty);

    internal static TimeSpan At(double seconds) => TimeSpan.FromSeconds(seconds);
}
