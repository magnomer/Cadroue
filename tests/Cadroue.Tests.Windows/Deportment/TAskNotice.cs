using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[CollectionDefinition("Schedule", DisableParallelization = true)]
public sealed class TScheduleCollection { }

[Collection("Schedule")]
public sealed class TAskNotice
{
    [Fact]
    public void Publish_RaisesQuestionAndRunsTheAnswer()
    {
        QAsk? seen = null;
        bool? answered = null;
        void TAskHandle(QAsk ask, Action<bool> answer)
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
        Assert.Equal("Delete it?", seen!.QAskQuestion);
        Assert.Equal("Delete", seen.QAskAction);
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
