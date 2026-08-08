using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class LJobTests
{
    [Fact]
    public void InputCollisionCheck_NormalizedSamePath_ReturnsTrue()
    {
        string pSource = Path.Combine(Path.GetTempPath(), "cadroue", "source.mp4");
        string pOutput = Path.Combine(Path.GetTempPath(), "cadroue", ".", "source.mp4");

        Assert.True(LJob.LJobCollisionCheck(pOutput, new[] { pSource }));
    }

    [Fact]
    public void InputCollisionCheck_DifferentPath_ReturnsFalse()
    {
        string pFolder = Path.Combine(Path.GetTempPath(), "cadroue");

        Assert.False(LJob.LJobCollisionCheck(
            Path.Combine(pFolder, "output.mp4"),
            new[] { Path.Combine(pFolder, "source.mp4") }));
    }

    [Fact]
    public void InputCollisionCheck_SameNameInDifferentFolder_ReturnsFalse()
    {
        string pRoot = Path.Combine(Path.GetTempPath(), "cadroue");

        Assert.False(LJob.LJobCollisionCheck(
            Path.Combine(pRoot, "output", "source.mp4"),
            new[] { Path.Combine(pRoot, "input", "source.mp4") }));
    }

    [Fact]
    public void InputCollisionCheck_WindowsCaseDifference_ReturnsTrue()
    {
        string pSource = Path.Combine(Path.GetTempPath(), "cadroue", "Source.mp4");
        string pOutput = Path.Combine(Path.GetTempPath(), "CADROUE", "source.MP4");

        Assert.True(LJob.LJobCollisionCheck(pOutput, new[] { pSource }));
    }

    [Fact]
    public void InputCollisionCheck_AnyMergeInputMatches_ReturnsTrue()
    {
        string pFolder = Path.Combine(Path.GetTempPath(), "cadroue");
        string pOutput = Path.Combine(pFolder, "second.mp4");

        Assert.True(LJob.LJobCollisionCheck(pOutput, new[]
        {
            Path.Combine(pFolder, "first.mp4"),
            pOutput
        }));
    }
}
