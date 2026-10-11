using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TLineageEntries
{
    [Fact]
    public void Read_GroupsAThreeStageBatchIntoOneEntry_WithTitleStepsAndRatios()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batch, "one");
        TScheduleWork second = schedule.TWorkCreate(batch, "two", parent: first);
        TScheduleWork third = schedule.TWorkCreate(batch, "three", parent: second);
        first.TWorkItem.LWorkSourceBytes = 1000;
        first.TWorkItem.LWorkOutputBytes = 800;
        second.TWorkItem.LWorkOutputBytes = 400;
        third.TWorkItem.LWorkOutputBytes = 200;
        LWorkItem[] items = [first.TWorkItem, second.TWorkItem, third.TWorkItem];

        IReadOnlyList<LLineageEntry> lineages = TInterfaceRoster.TLineageRead(items, _ => batch);
        LLineageEntry entry = Assert.Single(lineages);

        Assert.Equal(batch, entry.LLineageEntryBatch);
        Assert.Equal(3, entry.LLineageEntryItems.Count);
        Assert.Equal(first.TWorkItem.LWorkSourcePath, entry.LLineageEntrySubject);
        Assert.Equal(1000, entry.LLineageEntryOrigin);
        Assert.NotEmpty(TInterfaceRoster.TLineageTitleFormat(entry));
        Assert.Equal(1, TInterfaceRoster.TLineageInitialRead(items));
        Assert.Equal(new[] { second.TWorkId }, TInterfaceRoster.TLineageStageRead(items));

        string[] steps = items
            .Select(item => TInterfaceRoster.TLineageStepFormat(item, entry.LLineageEntrySubject))
            .ToArray();
        Assert.All(steps, Assert.NotEmpty);
        Assert.Equal(steps[0], steps[1]);
        Assert.StartsWith("80", TInterfaceRoster.TLineageRatioFormat(
            first.TWorkItem, entry.LLineageEntrySubject, 1000));
        Assert.StartsWith("40", TInterfaceRoster.TLineageRatioFormat(
            second.TWorkItem, entry.LLineageEntrySubject, 1000));
        Assert.StartsWith("20", TInterfaceRoster.TLineageRatioFormat(
            third.TWorkItem, entry.LLineageEntrySubject, 1000));
        Assert.Equal("-", TInterfaceRoster.TLineageRatioFormat(third.TWorkItem, entry.LLineageEntrySubject, null));
    }

    [Fact]
    public void Read_SeparatesBatches_AndKeepsFirstSeenOrder()
    {
        using var schedule = new TSchedule();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        TScheduleWork one = schedule.TWorkCreate(first, "one");
        TScheduleWork two = schedule.TWorkCreate(second, "two");
        TScheduleWork three = schedule.TWorkCreate(first, "three");

        IReadOnlyList<LLineageEntry> lineages = TInterfaceRoster.TLineageRead(
            [one.TWorkItem, two.TWorkItem, three.TWorkItem], item => item.LWorkBatchId);

        Assert.Equal(2, lineages.Count);
        Assert.Equal(first, lineages[0].LLineageEntryBatch);
        Assert.Equal(2, lineages[0].LLineageEntryItems.Count);
        Assert.Equal(second, lineages[1].LLineageEntryBatch);
        Assert.Null(lineages[1].LLineageEntryOrigin);
        Assert.Equal(one.TWorkItem.LWorkSourcePath, TInterfaceRoster.TLineagePathRead(one.TWorkItem.LWorkSourcePath));
        Assert.Null(TInterfaceRoster.TLineagePathRead(" "));
    }
}
