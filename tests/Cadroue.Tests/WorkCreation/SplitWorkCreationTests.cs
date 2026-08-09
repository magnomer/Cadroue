using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class SplitWorkCreationTests
{
    private static readonly string SourcePath = Path.Combine("media", "source.mov");

    [Fact]
    public void NoSource_ProducesNoWork()
    {
        IReadOnlyList<LWorkItem> work = Create(null, Section(1, 2, "First"));

        Assert.Empty(work);
    }

    [Fact]
    public void NoSections_ProducesNoWork()
    {
        IReadOnlyList<LWorkItem> work = Create(SourcePath);

        Assert.Empty(work);
    }

    [Fact]
    public void HiddenSections_ProduceNoWorkItems()
    {
        IReadOnlyList<LWorkItem> work = Create(SourcePath, Section(1, 2, "Hidden", hidden: true));

        Assert.Empty(work);
    }

    [Fact]
    public void EmptyAndReversedSections_AreSkipped()
    {
        IReadOnlyList<LWorkItem> work = Create(
            SourcePath,
            Section(2, 2, "Empty"),
            Section(5, 3, "Reversed"),
            Section(6, 8, "Valid"));

        LWorkItem item = Assert.Single(work);
        Assert.Equal("Valid.mp4", item.LWorkOutputName);
    }

    [Fact]
    public void EveryValidVisibleSection_ProducesExactlyOneWorkItem()
    {
        IReadOnlyList<LWorkItem> work = Create(
            SourcePath,
            Section(0, 1, "First"),
            Section(1, 2, "Hidden", hidden: true),
            Section(2, 4, "Second"),
            Section(5, 5, "Empty"));

        Assert.Equal(2, work.Count);
        Assert.All(work, item => Assert.Equal(LWorkKind.LWorkKindSplit, item.LWorkKind));
    }

    [Fact]
    public void SectionPositions_AreTransferredWithoutModification()
    {
        TimeSpan start = TimeSpan.FromTicks(12_345_678);
        TimeSpan end = TimeSpan.FromTicks(98_765_432);

        LWorkItem item = Assert.Single(Create(SourcePath, TInterface.SplitSectionCreate(start, end, "Precise")));

        Assert.Equal(start, item.LWorkOrigin);
        Assert.Equal(end, item.LWorkEnd);
    }

    [Fact]
    public void SuppliedSectionName_IsRepresentedInOutputName()
    {
        LWorkItem item = Assert.Single(Create(SourcePath, Section(1, 2, "Opening Scene")));

        Assert.Contains("Opening Scene", item.LWorkOutputName, StringComparison.Ordinal);
    }

    [Fact]
    public void BlankSectionName_ReceivesProductionFallbackName()
    {
        LWorkItem item = Assert.Single(Create(SourcePath, Section(1, 2, "   ")));

        Assert.Equal("Section 1.mp4", item.LWorkOutputName);
    }

    [Fact]
    public void InvalidFilenameCharacters_AreSanitized()
    {
        LWorkItem item = Assert.Single(Create(SourcePath, Section(1, 2, "Bad:Name?")));

        Assert.Equal("Bad_Name_.mp4", item.LWorkOutputName);
    }

    [Fact]
    public void DuplicateGeneratedOutputNames_AreDisambiguated()
    {
        IReadOnlyList<LWorkItem> work = Create(
            SourcePath,
            Section(0, 1, "Repeated"),
            Section(1, 2, "Repeated"),
            Section(2, 3, "Repeated"));

        Assert.Equal(new[] { "Repeated.mp4", "Repeated_2.mp4", "Repeated_3.mp4" },
            work.Select(item => item.LWorkOutputName));
    }

    [Fact]
    public void OutputExtensionAndDirectory_MatchRequestedExportSettings()
    {
        string exportFolder = Path.Combine("exports", "finished");
        LEncoding output = Output(extension: ".mkv", folder: exportFolder);

        LWorkItem item = Assert.Single(Create(SourcePath, output, Section(1, 2, "Clip")));

        Assert.Equal("Clip.mkv", item.LWorkOutputName);
        Assert.Equal(Path.Combine(exportFolder, "Clip.mkv"), item.LWorkOutputPath);
    }

    [Fact]
    public void OneCreationRequest_AssignsSameNonemptyBatchIdentityToAllWork()
    {
        IReadOnlyList<LWorkItem> work = Create(
            SourcePath,
            Section(0, 1, "First"),
            Section(1, 2, "Second"));

        Assert.NotEqual(Guid.Empty, work[0].LWorkBatchId);
        Assert.All(work, item => Assert.Equal(work[0].LWorkBatchId, item.LWorkBatchId));
    }

    [Fact]
    public void ExplicitBatchIdentity_IsPreserved()
    {
        Guid batchId = Guid.NewGuid();

        LWorkItem item = Assert.Single(Create(SourcePath, Output(), batchId, Section(1, 2, "Clip")));

        Assert.Equal(batchId, item.LWorkBatchId);
    }

    [Fact]
    public void PriorityAndSourceIdentity_SurviveWorkCreation()
    {
        string source = Path.Combine("incoming", "identity.mov");

        LWorkItem item = Assert.Single(Create(
            source,
            Output(),
            Guid.Empty,
            LWorkPriority.LWorkPriorityHigh,
            Section(1, 2, "Clip")));

        Assert.Equal(LWorkPriority.LWorkPriorityHigh, item.LWorkPriority);
        Assert.Equal(source, item.LWorkSourcePath);
    }

    private static IReadOnlyList<LWorkItem> Create(string? source, params LSplitSectionDescription[] sections) =>
        Create(source, Output(), Guid.Empty, LWorkPriority.LWorkPriorityNormal, sections);

    private static IReadOnlyList<LWorkItem> Create(
        string? source,
        LEncoding output,
        params LSplitSectionDescription[] sections) =>
        Create(source, output, Guid.Empty, LWorkPriority.LWorkPriorityNormal, sections);

    private static IReadOnlyList<LWorkItem> Create(
        string? source,
        LEncoding output,
        Guid batchId,
        params LSplitSectionDescription[] sections) =>
        Create(source, output, batchId, LWorkPriority.LWorkPriorityNormal, sections);

    private static IReadOnlyList<LWorkItem> Create(
        string? source,
        LEncoding output,
        Guid batchId,
        LWorkPriority priority,
        params LSplitSectionDescription[] sections) =>
        TInterface.SplitItemsCreate(
            priority,
            TInterface.SplitDescriptionCreate(source, sections, output),
            "test-tab",
            _ => { },
            _ => { },
            batchId);

    private static LSplitSectionDescription Section(double start, double end, string name, bool hidden = false) =>
        TInterface.SplitSectionCreate(TimeSpan.FromSeconds(start), TimeSpan.FromSeconds(end), name, hidden);

    private static LEncoding Output(string extension = "mp4", string? folder = null) =>
        WorkCreationOutput.SplitCreate(extension, folder);
}
