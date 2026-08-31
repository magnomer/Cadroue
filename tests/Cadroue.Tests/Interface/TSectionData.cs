using Cadroue.Core;

namespace Cadroue.Tests;

internal static class TSectionData
{
    internal static LPiece TSegmentPieceCreate(double startSeconds, double endSeconds) =>
        new(TimeSpan.FromSeconds(startSeconds), TimeSpan.FromSeconds(endSeconds), 0, string.Empty);

    internal static TimeSpan TSegmentAtCreate(double seconds) => TimeSpan.FromSeconds(seconds);
}
