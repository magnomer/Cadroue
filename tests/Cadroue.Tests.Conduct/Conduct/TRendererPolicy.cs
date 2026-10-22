using System;
using Cadroue.Conduct;

using Xunit;

namespace Cadroue.Tests.Conduct;

public sealed class TRendererPolicy : CSRendererPort
{
    private CSRendererFact tRendererPolicyFact = new(false, false);
    private Exception? tRendererPolicyFailure;
    private int tRendererPolicyReads;

    [Fact]
    public void CSRendererRead()
    {
        var renderer = new CSRenderer(this);

        CSRendererSnapshot blank = renderer.CSRendererRead("  ");
        Assert.Equal("  ", blank.CSRendererSnapshotPath);
        Assert.Equal(CSRendererState.CSRendererStateBlank, blank.CSRendererSnapshotState);
        Assert.Equal(0, tRendererPolicyReads);

        (CSRendererFact, CSRendererState)[] states =
        [
            (new CSRendererFact(true, true), CSRendererState.CSRendererStateReady),
            (new CSRendererFact(true, false), CSRendererState.CSRendererStateProgram),
            (new CSRendererFact(false, true), CSRendererState.CSRendererStateLibrary),
            (new CSRendererFact(false, false), CSRendererState.CSRendererStateMissing)
        ];
        foreach (var state in states)
        {
            tRendererPolicyFact = state.Item1;
            CSRendererSnapshot snapshot = renderer.CSRendererRead(" raw ");
            Assert.Equal(" raw ", snapshot.CSRendererSnapshotPath);
            Assert.Equal(state.Item2, snapshot.CSRendererSnapshotState);
        }

        Assert.Equal(4, tRendererPolicyReads);
        tRendererPolicyFailure = new InvalidOperationException("forced");
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(
            () => renderer.CSRendererRead("fault"));
        Assert.Equal("forced", failure.Message);
    }

    CSRendererFact CSRendererPort.CSRendererPortRead(string path)
    {
        tRendererPolicyReads++;
        if (tRendererPolicyFailure is not null)
        {
            throw tRendererPolicyFailure;
        }

        return tRendererPolicyFact;
    }
}
