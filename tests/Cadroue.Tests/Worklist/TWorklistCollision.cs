using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TWorklistCollision
{
    [Fact]
    public void Output_NormalizedToInput_Collides()
    {
        string pSource = Path.Combine(Path.GetTempPath(), "cadroue", "source.mp4");
        string pOutput = Path.Combine(Path.GetTempPath(), "cadroue", ".", "source.mp4");

        Assert.True(TInterface.TJobCollisionCheck(pOutput, new[] { pSource }));
    }

    [Fact]
    public void Output_DifferentFromInput_DoesNotCollide()
    {
        string pFolder = Path.Combine(Path.GetTempPath(), "cadroue");

        Assert.False(TInterface.TJobCollisionCheck(
            Path.Combine(pFolder, "output.mp4"),
            new[] { Path.Combine(pFolder, "source.mp4") }));
    }

    [Fact]
    public void Output_SameNameInDifferentFolder_DoesNotCollide()
    {
        string pRoot = Path.Combine(Path.GetTempPath(), "cadroue");

        Assert.False(TInterface.TJobCollisionCheck(
            Path.Combine(pRoot, "output", "source.mp4"),
            new[] { Path.Combine(pRoot, "input", "source.mp4") }));
    }

    [Fact]
    public void Output_MatchingInputByCase_CollidesOnWindows()
    {
        string pSource = Path.Combine(Path.GetTempPath(), "cadroue", "Source.mp4");
        string pOutput = Path.Combine(Path.GetTempPath(), "CADROUE", "source.MP4");

        Assert.True(TInterface.TJobCollisionCheck(pOutput, new[] { pSource }));
    }

    [Fact]
    public void Output_MatchingAnyMergeInput_Collides()
    {
        string pFolder = Path.Combine(Path.GetTempPath(), "cadroue");
        string pOutput = Path.Combine(pFolder, "second.mp4");

        Assert.True(TInterface.TJobCollisionCheck(pOutput, new[]
        {
            Path.Combine(pFolder, "first.mp4"),
            pOutput
        }));
    }
}
