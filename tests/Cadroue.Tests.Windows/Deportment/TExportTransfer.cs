using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TExportTransfer
{
    [Fact]
    public void TransferResults_RefusalAndSuccess_DispatchExactlyOneOutcome()
    {
        LPresetDocument record = TInterface.TPresetDocumentCreate("Imported", "Imported");
        bool catalogue = false;
        bool written = true;
        bool saved = true;
        int loadMode = 0;
        int catalogueCalls = 0;
        int writes = 0;
        int reads = 0;
        int saves = 0;
        int selections = 0;
        LPresetDocument? exported = null;
        QExportTransfer transfer = TInterface.TInterfaceExportRead(
            () => record,
            () => "Selected",
            _ =>
            {
                catalogueCalls++;
                return catalogue;
            },
            (value, _) =>
            {
                writes++;
                exported = value;
                return written;
            },
            _ =>
            {
                reads++;
                return loadMode switch
                {
                    1 => throw new InvalidOperationException("broken"),
                    2 => null,
                    _ => record
                };
            },
            (name, _) => name,
            name => name,
            (_, _) =>
            {
                saves++;
                return saved;
            },
            _ => selections++,
            name => name);
        var failures = new List<string>();
        var refusals = new List<(string, string?)>();
        Action<string> failure = failures.Add;
        Action<string, string?> refusal = (name, detail) => refusals.Add((name, detail));
        Action clear = () =>
        {
            failures.Clear();
            refusals.Clear();
        };

        foreach (bool? confirmed in new bool?[] { false, null })
        {
            TInterface.TInterfaceExportRead(transfer, confirmed, "cancel-save.json", failure, refusal);
            TInterface.TInterfaceExportRead(transfer, confirmed, "cancel-import.json", "Fallback", failure, refusal);
        }
        Assert.Equal(0, catalogueCalls + writes + reads + saves + selections);
        Assert.Empty(failures);
        Assert.Empty(refusals);

        catalogue = true;
        TInterface.TInterfaceExportRead(transfer, true, "catalogue.json", failure, refusal);
        Assert.Equal([("catalogue.json", null)], refusals);
        Assert.Empty(failures);
        Assert.Equal(0, writes);
        clear();

        catalogue = false;
        written = false;
        TInterface.TInterfaceExportRead(transfer, true, "write.json", failure, refusal);
        Assert.Equal(["write.json"], failures);
        Assert.Empty(refusals);
        clear();

        LPresetDocument original = record;
        LPresetDocument replacement = TInterface.TPresetDocumentCreate("Replacement", "Replacement");
        written = true;
        TInterface.TInterfaceExportRead(
            transfer,
            true,
            "prepared.json",
            failure,
            refusal,
            () => record = replacement);
        Assert.Same(original, exported);
        Assert.Empty(failures);
        Assert.Empty(refusals);
        record = original;

        loadMode = 1;
        TInterface.TInterfaceExportRead(transfer, true, "read.json", "Fallback", failure, refusal);
        Assert.Equal([("read.json", "broken")], refusals);
        Assert.Empty(failures);
        clear();

        loadMode = 2;
        TInterface.TInterfaceExportRead(transfer, true, "invalid.json", "Fallback", failure, refusal);
        Assert.Equal([("invalid.json", null)], refusals);
        Assert.Empty(failures);
        clear();

        loadMode = 0;
        saved = false;
        TInterface.TInterfaceExportRead(transfer, true, "save.json", "Fallback", failure, refusal);
        Assert.Equal(["Imported"], failures);
        Assert.Empty(refusals);
        Assert.Equal(0, selections);
        clear();

        saved = true;
        TInterface.TInterfaceExportRead(transfer, true, "success.json", "Fallback", failure, refusal);
        Assert.Empty(failures);
        Assert.Empty(refusals);
        Assert.Equal(1, selections);
    }

    [Fact]
    public void Projection_FollowsOwnerWithoutSync()
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                using TPreset presets = new();
                presets.TPresetSeedCreate("Alpha", "Beta");
                LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
                LPresetDocument alpha = TInterface.TPresetDocumentCreate("Alpha", "Alpha output");
                alpha.LPresetContainer = "Alpha container";
                alpha.LPresetExtension = "alpha";
                alpha.LPresetVideo.LPresetMode = "Encode";
                alpha.LPresetVideo.LPresetStream = "Alpha video";
                alpha.LPresetAudio.LPresetMode = "Encode";
                alpha.LPresetAudio.LPresetStream = "Alpha audio";
                selection.LPresetSelectionValue = alpha;
                QExport export = TInterface.TExportCreate(selection, "Convert");
                LPresetDocument beta = TInterface.TPresetDocumentCreate("Beta", "Beta output");
                beta.LPresetContainer = "Beta container";
                beta.LPresetExtension = "beta";
                beta.LPresetVideo.LPresetMode = "Copy";
                beta.LPresetVideo.LPresetStream = "Beta video";
                beta.LPresetAudio.LPresetMode = "Copy";
                beta.LPresetAudio.LPresetStream = "Beta audio";
                TInterface.TExportSelect(export, "Beta");
                selection.LPresetSelectionValue = beta;
                TInterface.TExportSelect(export, "Alpha");
                int[] routes = new int[5];
                var (surface, iconState) = TInterface.TInterfaceExportRead(export, selection, routes);

                try
                {
                    object[] initial = TInterface.TInterfaceExportRead(surface);
                    QExportDigest first = TInterface.TExportSummaryRead(export).Item2;
                    QExportSummary summary = export.QExportSummary;
                    Assert.IsType<ScrollViewer>(initial[0]);
                    Assert.IsType<Grid>(initial[1]);
                    Assert.Equal("PExportSummary", initial[2].GetType().Name);
                    Assert.Equal(0, initial[3]);
                    Assert.Equal(1, initial[4]);
                    Assert.Equal(3, initial[5]);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("ExportSummary.Header.Export"),
                        Assert.IsType<TextBlock>(initial[6]).Text);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("ExportSummary.Header.Summary"),
                        Assert.IsType<TextBlock>(initial[7]).Text);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("Roster.Field.Container"),
                        Assert.IsType<TextBlock>(initial[8]).Text);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("ExportSummary.Video"),
                        Assert.IsType<TextBlock>(initial[9]).Text);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("ExportSummary.Audio"),
                        Assert.IsType<TextBlock>(initial[10]).Text);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("Roster.Section.Output"),
                        Assert.IsType<TextBlock>(initial[11]).Text);
                    Assert.Equal(first.QExportDigestContainer, Assert.IsType<TextBlock>(initial[12]).Text);
                    Assert.Equal(first.QExportDigestVideo, Assert.IsType<TextBlock>(initial[13]).Text);
                    Assert.Equal(first.QExportDigestAudio, Assert.IsType<TextBlock>(initial[14]).Text);
                    Assert.Equal(first.QExportDigestOutput, Assert.IsType<TextBlock>(initial[15]).Text);
                    object[] actions = TInterface.TInterfaceExportRead(surface, 0);
                    Grid actionGrid = Assert.IsType<Grid>(actions[0]);
                    Assert.Equal([GridLength.Auto, new(1, GridUnitType.Star), GridLength.Auto],
                        actionGrid.ColumnDefinitions.Select(column => column.Width));
                    string[] names =
                    [
                        "PExportCreation",
                        "PExportDeletion",
                        "PExportConfiguration",
                        "PExportOutbound",
                        "PExportInbound"
                    ];
                    string[] tooltips =
                    [
                        "ExportPreset.AddTooltip",
                        "ExportPreset.DeleteTooltip",
                        "ExportPreset.SettingsTooltip",
                        "ExportPreset.ExportTooltip",
                        "ExportPreset.ImportTooltip"
                    ];
                    string[] icons =
                    [
                        "/PAsset/PPanel/PExportPlus.svg",
                        "/PAsset/PPanel/PExportMinus.svg",
                        "/PAsset/PPanel/PExportSetting.svg",
                        "/PAsset/PPanel/PExportExport.svg",
                        "/PAsset/PPanel/PExportImport.svg"
                    ];
                    for (int index = 0; index < names.Length; index++)
                    {
                        Button button = Assert.IsType<Button>(actions[index + 1]);
                        Assert.Equal(names[index], button.Name);
                        Assert.Equal(TInterface.TLocalizationCatalogRead(tooltips[index]), button.ToolTip);
                        Assert.Equal(28, button.Width);
                        Assert.Equal(26, button.Height);
                        Assert.Equal(new(0, 0, 2, 0), button.Margin);
                        Image image = Assert.IsType<Image>(button.Content);
                        Assert.Equal(20, image.Width);
                        Assert.Equal(20, image.Height);
                        Assert.Contains(icons[index], Assert.IsType<string>(actions[index + 6]),
                            StringComparison.Ordinal);
                        Assert.Equal(1, actions[index + 11]);
                        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    Assert.Equal([1, 1, 1, 1, 1], routes);

                    int refreshes = 0;
                    QExportDigest? refreshed = null;
                    TInterface.TExportAttach(export, () =>
                    {
                        refreshes++;
                        refreshed = TInterface.TExportSummaryRead(export).Item2;
                    });
                    TInterface.TInterfaceExportRead(surface, true);
                    Assert.Equal(1, refreshes);
                    refreshes = 0;

                    string firstVideo = first.QExportDigestVideo;
                    string firstAudio = first.QExportDigestAudio;
                    string firstContainer = first.QExportDigestContainer;
                    string firstOutput = first.QExportDigestOutput;
                    LPresetDocument draft = TInterface.TPresetDocumentCreate("Alpha", "Draft output");
                    draft.LPresetContainer = "Draft container";
                    draft.LPresetExtension = "draft";
                    draft.LPresetVideo.LPresetMode = "Encode";
                    draft.LPresetVideo.LPresetStream = "Draft video";
                    draft.LPresetAudio.LPresetMode = "Encode";
                    draft.LPresetAudio.LPresetStream = "Draft audio";
                    selection.LPresetSelectionValue = draft;
                    QExportDigest current = TInterface.TExportSummaryRead(export).Item2;
                    object[] drafted = TInterface.TInterfaceExportRead(surface);
                    Assert.Equal(1, refreshes);
                    Assert.Equal(current, refreshed);
                    Assert.NotEqual(first, current);
                    Assert.Equal(firstVideo, first.QExportDigestVideo);
                    Assert.Equal(firstAudio, first.QExportDigestAudio);
                    Assert.Equal(firstContainer, first.QExportDigestContainer);
                    Assert.Equal(firstOutput, first.QExportDigestOutput);
                    Assert.Equal(current.QExportDigestContainer, Assert.IsType<TextBlock>(drafted[12]).Text);
                    Assert.Equal(current.QExportDigestVideo, Assert.IsType<TextBlock>(drafted[13]).Text);
                    Assert.Equal(current.QExportDigestAudio, Assert.IsType<TextBlock>(drafted[14]).Text);
                    Assert.Equal(current.QExportDigestOutput, Assert.IsType<TextBlock>(drafted[15]).Text);

                    refreshes = 0;
                    TInterface.TExportSelect(export, "Beta");
                    QExportDigest selected = TInterface.TExportSummaryRead(export).Item2;
                    object[] selectedFace = TInterface.TInterfaceExportRead(surface);
                    Assert.Equal(1, refreshes);
                    Assert.Equal(selected, refreshed);
                    Assert.Equal(selected.QExportDigestContainer, Assert.IsType<TextBlock>(selectedFace[12]).Text);
                    Assert.Equal(selected.QExportDigestVideo, Assert.IsType<TextBlock>(selectedFace[13]).Text);
                    Assert.Equal(selected.QExportDigestAudio, Assert.IsType<TextBlock>(selectedFace[14]).Text);
                    Assert.Equal(selected.QExportDigestOutput, Assert.IsType<TextBlock>(selectedFace[15]).Text);

                    TInterface.TInterfaceExportRead(surface, false);
                    refreshes = 0;
                    LPresetDocument detached = TInterface.TPresetDocumentCreate("Beta", "Detached output");
                    detached.LPresetContainer = "Detached container";
                    detached.LPresetExtension = "detached";
                    detached.LPresetVideo.LPresetMode = "Encode";
                    detached.LPresetVideo.LPresetStream = "Detached video";
                    detached.LPresetAudio.LPresetMode = "Encode";
                    detached.LPresetAudio.LPresetStream = "Detached audio";
                    selection.LPresetSelectionValue = detached;
                    Assert.Equal(0, refreshes);
                    Assert.Equal(selected.QExportDigestContainer,
                        Assert.IsType<TextBlock>(TInterface.TInterfaceExportRead(surface)[12]).Text);

                    TInterface.TInterfaceExportRead(surface, true);
                    QExportDigest reloaded = TInterface.TExportSummaryRead(export).Item2;
                    object[] reloadedFace = TInterface.TInterfaceExportRead(surface);
                    Assert.Equal(1, refreshes);
                    Assert.Same(initial[0], reloadedFace[0]);
                    Assert.Same(initial[1], reloadedFace[1]);
                    Assert.Same(initial[2], reloadedFace[2]);
                    Assert.Same(summary, export.QExportSummary);
                    object[] reloadedActions = TInterface.TInterfaceExportRead(surface, 0);
                    for (int index = 0; index < names.Length; index++)
                    {
                        Assert.Same(actions[index + 1], reloadedActions[index + 1]);
                        Assert.Equal(1, reloadedActions[index + 11]);
                        ((Button)reloadedActions[index + 1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    Assert.Equal([2, 2, 2, 2, 2], routes);
                    Assert.Equal(reloaded.QExportDigestContainer, Assert.IsType<TextBlock>(reloadedFace[12]).Text);
                    Assert.Equal(reloaded.QExportDigestVideo, Assert.IsType<TextBlock>(reloadedFace[13]).Text);
                    Assert.Equal(reloaded.QExportDigestAudio, Assert.IsType<TextBlock>(reloadedFace[14]).Text);
                    Assert.Equal(reloaded.QExportDigestOutput, Assert.IsType<TextBlock>(reloadedFace[15]).Text);

                    refreshes = 0;
                    detached.LPresetDisplay = "Reloaded output";
                    detached.LPresetContainer = "Reloaded container";
                    selection.LPresetSelectionValue = detached;
                    QExportDigest afterReload = TInterface.TExportSummaryRead(export).Item2;
                    object[] afterReloadFace = TInterface.TInterfaceExportRead(surface);
                    Assert.Equal(1, refreshes);
                    Assert.Equal(afterReload, refreshed);
                    Assert.Equal(afterReload.QExportDigestContainer,
                        Assert.IsType<TextBlock>(afterReloadFace[12]).Text);
                    Assert.Equal(afterReload.QExportDigestOutput,
                        Assert.IsType<TextBlock>(afterReloadFace[15]).Text);
                }
                finally
                {
                    TInterface.TInterfaceExportRead(surface, false, iconState);
                    TInterface.TPresetSelectionClose(selection);
                }
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
