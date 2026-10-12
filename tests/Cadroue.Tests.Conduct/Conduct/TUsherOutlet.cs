using System.Collections.Generic;
using Cadroue.ShellEngine;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

public sealed class TUsherOutlet
{
    [Fact]
    public void UsherPathOpen_FailureText_RecordsTheCallerTextWithTheError()
    {
        List<string> traces = [];
        LUsherEngine.LUsherRevealSeam = (path, fallback) => $"{path}|{fallback}.failed";
        LTraceEngine.LTraceErrorSeam = (message, _) => traces.Add(message);
        try
        {
            Assert.Equal("{a}|.failed", TInterface.TUsherPathOpen("{a}", null, "Could not open '{0}': {1}"));
        }
        finally
        {
            LUsherEngine.LUsherRevealSeam = (_, _) => null;
            LTraceEngine.LTraceErrorSeam = (_, _) => { };
        }

        Assert.Equal(["Could not open '{a}': {a}|.failed"], traces);
    }

    [Fact]
    public void UsherPathOpen_WithoutFailureText_RecordsNothing()
    {
        List<string> traces = [];
        LUsherEngine.LUsherRevealSeam = (path, fallback) => $"{path}|{fallback}.failed";
        LTraceEngine.LTraceErrorSeam = (message, _) => traces.Add(message);
        try
        {
            Assert.Equal("a|.failed", TInterface.TUsherPathOpen("a", null, null));
        }
        finally
        {
            LUsherEngine.LUsherRevealSeam = (_, _) => null;
            LTraceEngine.LTraceErrorSeam = (_, _) => { };
        }

        Assert.Empty(traces);
    }

    [Fact]
    public void UsherPathOpen_Success_RecordsNothing()
    {
        List<string> traces = [];
        LTraceEngine.LTraceErrorSeam = (message, _) => traces.Add(message);
        try
        {
            Assert.Null(TInterface.TUsherPathOpen("a", null, "Could not open '{0}': {1}"));
        }
        finally
        {
            LTraceEngine.LTraceErrorSeam = (_, _) => { };
        }

        Assert.Empty(traces);
    }

    [Fact]
    public void UsherLinkOpen_FailureText_RecordsTheCallerTextWithTheError()
    {
        List<string> traces = [];
        LUsherEngine.LUsherLinkSeam = target => target + ".failed";
        LTraceEngine.LTraceErrorSeam = (message, _) => traces.Add(message);
        try
        {
            Assert.Equal("{t}.failed", TInterface.TUsherLinkOpen("{t}", "Link could not be opened: {0}: {1}"));
        }
        finally
        {
            LUsherEngine.LUsherLinkSeam = _ => null;
            LTraceEngine.LTraceErrorSeam = (_, _) => { };
        }

        Assert.Equal(["Link could not be opened: {t}: {t}.failed"], traces);
    }

    [Fact]
    public void UsherLinkOpen_WithoutFailureText_RecordsNothing()
    {
        List<string> traces = [];
        LUsherEngine.LUsherLinkSeam = target => target + ".failed";
        LTraceEngine.LTraceErrorSeam = (message, _) => traces.Add(message);
        try
        {
            Assert.Equal("t.failed", TInterface.TUsherLinkOpen("t", null));
        }
        finally
        {
            LUsherEngine.LUsherLinkSeam = _ => null;
            LTraceEngine.LTraceErrorSeam = (_, _) => { };
        }

        Assert.Empty(traces);
    }

    [Fact]
    public void UsherTextReads_Path_AskTheTextSeams()
    {
        LUsherTextEngine.LUsherNameSeam = path => path + ".name";
        LUsherTextEngine.LUsherStemSeam = path => path + ".stem";
        try
        {
            Assert.Equal("a.name", TInterface.TUsherNameRead("a"));
            Assert.Equal("a.stem", TInterface.TUsherStemRead("a"));
        }
        finally
        {
            LUsherTextEngine.LUsherNameSeam = _ => string.Empty;
            LUsherTextEngine.LUsherStemSeam = _ => string.Empty;
        }
    }
}
