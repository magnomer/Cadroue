using System;
using System.Collections.Generic;

using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class SalvagePlanTests
{
    private const string SourcePath = @"C:\vids\clip.mp4";

    private static LSalvageSpan Span(double startSeconds, double endSeconds) =>
        new(TimeSpan.FromSeconds(startSeconds), TimeSpan.FromSeconds(endSeconds));

    [Fact]
    public void EmptySpans_ProduceEmptyPlan()
    {
        IReadOnlyList<LSalvageOutput> plan = LSalvage.LSalvagePlanCreate(
            Array.Empty<LSalvageSpan>(), LSalvageMode.LSalvageModeSeparate, SourcePath,
            WorkCreationOutput.Create(folder: @"C:\out"));

        Assert.Empty(plan);
    }

    [Fact]
    public void Rejoin_MultipleSpans_ProduceOneOutputOverWholeRange()
    {
        var spans = new[] { Span(0, 10), Span(20, 30), Span(40, 55) };

        IReadOnlyList<LSalvageOutput> plan = LSalvage.LSalvagePlanCreate(
            spans, LSalvageMode.LSalvageModeRejoin, SourcePath,
            WorkCreationOutput.Create(folder: @"C:\out"));

        LSalvageOutput only = Assert.Single(plan);
        Assert.Equal("clip.mp4", only.LSalvageOutputName);
        Assert.Equal(TimeSpan.FromSeconds(0), only.LSalvageOutputSpan.LSalvageSpanOrigin);
        Assert.Equal(TimeSpan.FromSeconds(55), only.LSalvageOutputSpan.LSalvageSpanLimit);
    }

    [Fact]
    public void SingleSpan_Separate_ProduceOneOutput()
    {
        var spans = new[] { Span(5, 25) };

        IReadOnlyList<LSalvageOutput> plan = LSalvage.LSalvagePlanCreate(
            spans, LSalvageMode.LSalvageModeSeparate, SourcePath,
            WorkCreationOutput.Create(folder: @"C:\out"));

        LSalvageOutput only = Assert.Single(plan);
        Assert.Equal("clip.mp4", only.LSalvageOutputName);
        Assert.Equal(spans[0], only.LSalvageOutputSpan);
    }

    [Fact]
    public void Separate_NonTokenPattern_AppendsNumberSuffix()
    {
        var spans = new[] { Span(0, 10), Span(20, 30), Span(40, 50) };

        IReadOnlyList<LSalvageOutput> plan = LSalvage.LSalvagePlanCreate(
            spans, LSalvageMode.LSalvageModeSeparate, SourcePath,
            WorkCreationOutput.Create(pattern: "{OriginalName}", folder: @"C:\out"));

        Assert.Collection(plan,
            output => Assert.Equal("clip (1).mp4", output.LSalvageOutputName),
            output => Assert.Equal("clip (2).mp4", output.LSalvageOutputName),
            output => Assert.Equal("clip (3).mp4", output.LSalvageOutputName));
        Assert.Equal(spans[1], plan[1].LSalvageOutputSpan);
    }

    [Fact]
    public void Separate_TokenPattern_UsesSectionNumber()
    {
        var spans = new[] { Span(0, 10), Span(20, 30) };

        IReadOnlyList<LSalvageOutput> plan = LSalvage.LSalvagePlanCreate(
            spans, LSalvageMode.LSalvageModeSeparate, SourcePath,
            WorkCreationOutput.Create(pattern: "{OriginalName}-{SectionNumber}", folder: @"C:\out"));

        Assert.Collection(plan,
            output => Assert.Equal("clip-01.mp4", output.LSalvageOutputName),
            output => Assert.Equal("clip-02.mp4", output.LSalvageOutputName));
    }

    [Fact]
    public void Separate_SectionNameOnlyPattern_DeduplicatesCollisions()
    {
        var spans = new[] { Span(0, 10), Span(20, 30) };

        IReadOnlyList<LSalvageOutput> plan = LSalvage.LSalvagePlanCreate(
            spans, LSalvageMode.LSalvageModeSeparate, SourcePath,
            WorkCreationOutput.Create(pattern: "{SectionName}", folder: @"C:\out"));

        Assert.Equal(2, plan.Count);
        Assert.NotEqual(plan[0].LSalvageOutputName, plan[1].LSalvageOutputName);
    }

    [Fact]
    public void SingleOutput_NameEqualsSource_GetsFixSuffix()
    {
        var spans = new[] { Span(0, 30) };

        IReadOnlyList<LSalvageOutput> plan = LSalvage.LSalvagePlanCreate(
            spans, LSalvageMode.LSalvageModeRejoin, SourcePath,
            WorkCreationOutput.Create(pattern: "{OriginalName}"));

        LSalvageOutput only = Assert.Single(plan);
        Assert.Equal("clip_fix.mp4", only.LSalvageOutputName);
    }
}
