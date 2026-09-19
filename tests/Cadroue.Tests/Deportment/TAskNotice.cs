using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TAskNotice
{
    [Fact]
    public void Publish_RaisesQuestionAndRunsTheAnswer()
    {
        LAsk? seen = null;
        bool? answered = null;
        void TAskHandle(LAsk ask, Action<bool> answer)
        {
            seen = ask;
            answer(true);
        }

        TInterface.TAskAttach(TAskHandle);
        try
        {
            TInterface.TAskPublish(TInterface.TAskCreate("Delete it?", "Delete"), result => answered = result);
        }
        finally
        {
            TInterface.TAskDetach(TAskHandle);
        }

        Assert.NotNull(seen);
        Assert.Equal("Delete it?", seen!.LAskQuestion);
        Assert.Equal("Delete", seen.LAskAction);
        Assert.True(answered);
    }

    [Fact]
    public void Publish_WithoutQuestion_RunsAtOnce()
    {
        bool? answered = null;

        TInterface.TAskPublish(null, result => answered = result);

        Assert.True(answered);
    }

    [Fact]
    public void Publish_WithoutSubscriber_Declines()
    {
        bool? answered = null;

        TInterface.TAskPublish(TInterface.TAskCreate("Delete it?", "Delete"), result => answered = result);

        Assert.False(answered);
    }
}
