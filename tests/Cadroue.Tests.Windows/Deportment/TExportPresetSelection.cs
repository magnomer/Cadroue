using System.Runtime.ExceptionServices;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TExportPresetSelection
{
    [Fact]
    public void Select_ChangesOwner_RebuildsRows()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha", "Beta");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        QExport export = TInterface.TExportCreate(selection, "Split");
        TInterface.TExportOwnerAttach(export);
        int rebuilds = 0;
        TInterface.TExportAttach(export, () => rebuilds++);

        TInterface.TExportSelect(export, "Beta");

        Assert.Equal("Beta", export.QExportSelected);
        Assert.Equal("Beta", selection.LPresetSelectionName);
        Assert.Equal(1, rebuilds);
    }

    [Fact]
    public void EditStart_SelectsAndMarksEditing_CancelClears()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha", "Beta");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        QExport export = TInterface.TExportCreate(selection, "Convert");
        TInterface.TExportOwnerAttach(export);
        int rebuilds = 0;
        TInterface.TExportAttach(export, () => rebuilds++);

        TInterface.TExportEditStart(export, "Beta");

        Assert.Equal("Beta", export.QExportSelected);
        Assert.Equal("Beta", export.QExportEditing);
        Assert.Equal(2, rebuilds);

        TInterface.TExportSync(export);
        Assert.Equal(2, rebuilds);

        TInterface.TExportEditCancel(export);

        Assert.Null(export.QExportEditing);
        Assert.Equal(3, rebuilds);
    }

    [Fact]
    public void NameCommit_OnlyForEditedRule_RenamesSelection()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha", "Beta");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        QExport export = TInterface.TExportCreate(selection, "Convert");
        TInterface.TExportEditStart(export, "Alpha");

        Assert.False(TInterface.TExportNameCommit(export, "Beta", "Gamma"));
        Assert.True(TInterface.TExportNameCommit(export, "Alpha", "Gamma"));

        Assert.Null(export.QExportEditing);
        Assert.Equal("Gamma", selection.LPresetSelectionName);
        Assert.Equal("Gamma", export.QExportSelected);
    }

    [Fact]
    public void Drag_DefersSync_UntilCleared()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha", "Beta");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        QExport export = TInterface.TExportCreate(selection, "Convert");
        int rebuilds = 0;
        TInterface.TExportAttach(export, () => rebuilds++);

        Assert.False(TInterface.TExportDragMove(export));
        TInterface.TExportDragStart(export, "Alpha");
        Assert.True(TInterface.TExportDragMove(export));
        Assert.True(export.QExportDrag.QExportDragActive);

        TInterface.TExportSync(export);
        Assert.Equal(0, rebuilds);

        Assert.True(TInterface.TExportDragClear(export));
        Assert.False(TInterface.TExportDragMove(export));
        Assert.Equal(1, rebuilds);
        Assert.False(TInterface.TExportDragClear(export));
    }

    [Fact]
    public void PresetAdd_PublishesOneRepaintOrFailure()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        QExport export = TInterface.TExportCreate(selection, "Convert");
        TInterface.TExportOwnerAttach(export);
        int rebuilds = 0;
        var failures = new List<string>();
        TInterface.TExportAttach(export, () => rebuilds++);
        Func<string, LPresetDocument, bool>? save = LPresetSelection.LPresetSaveSeam;
        LPresetSelection.LPresetSaveSeam = LPresetTable.LPresetSelectionSave;

        try
        {
            TInterface.TExportPresetAdd(export, "Beta", failures.Add);

            Assert.Equal("Beta", export.QExportSelected);
            Assert.Equal(1, rebuilds);
            Assert.Empty(failures);

            var attempted = new List<string>();
            LPresetSelection.LPresetSaveSeam = (name, _) =>
            {
                attempted.Add(name);
                return false;
            };
            TInterface.TExportPresetAdd(export, "Beta", failures.Add);

            Assert.Equal("Beta", export.QExportSelected);
            Assert.Equal(1, rebuilds);
            Assert.Single(attempted);
            Assert.NotEqual("Beta", attempted[0]);
            Assert.Equal(attempted, failures);
        }
        finally
        {
            LPresetSelection.LPresetSaveSeam = save;
        }
    }

    [Fact]
    public void PresetDeleteFailure_ReportsSelectionWithoutRepaint()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha", "Beta");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        Func<Func<LPresetCatalog, IReadOnlyList<LPresetDocument>?>, bool>? persist =
            LPresetPersistence.LPresetSaveSeam;
        Func<string, LPresetDocument, bool>? save = LPresetSelection.LPresetSaveSeam;
        LPresetPersistence.LPresetSaveSeam = null;
        LPresetSelection.LPresetSaveSeam = LPresetTable.LPresetSelectionSave;
        QExport export = TInterface.TExportCreate(selection, "Convert");
        TInterface.TExportOwnerAttach(export);
        TInterface.TExportModificationApply(export, _ => Assert.Fail("Preset setup failed."));
        int rebuilds = 0;
        var failures = new List<string>();
        TInterface.TExportAttach(export, () => rebuilds++);
        LPresetPersistence.LPresetSaveSeam = _ => false;

        try
        {
            TInterface.TExportPresetDelete(export, failures.Add);

            Assert.Contains("Alpha", LPresetTable.LPresetNames);
            Assert.Equal(0, rebuilds);
            Assert.Equal(["Alpha"], failures);

            TInterface.TExportSelect(export, "Alpha");
            rebuilds = 0;
            failures.Clear();
            LPresetPersistence.LPresetSaveSeam = null;

            TInterface.TExportPresetDelete(export, failures.Add);

            Assert.DoesNotContain("Alpha", LPresetTable.LPresetNames);
            Assert.Equal(1, rebuilds);
            Assert.Empty(failures);
        }
        finally
        {
            LPresetSelection.LPresetSaveSeam = save;
            LPresetPersistence.LPresetSaveSeam = persist;
        }
    }

    [Fact]
    public void ModificationApplyFailure_ReportsSelectionWithoutRepaint()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        Func<Func<LPresetCatalog, IReadOnlyList<LPresetDocument>?>, bool>? persist =
            LPresetPersistence.LPresetSaveSeam;
        Func<string, LPresetDocument, bool>? save = LPresetSelection.LPresetSaveSeam;
        LPresetPersistence.LPresetSaveSeam = null;
        LPresetSelection.LPresetSaveSeam = LPresetTable.LPresetSelectionSave;
        QExport export = TInterface.TExportCreate(selection, "Convert");
        TInterface.TExportOwnerAttach(export);
        TInterface.TExportModificationApply(export, _ => Assert.Fail("Preset setup failed."));
        int rebuilds = 0;
        var failures = new List<string>();
        TInterface.TExportAttach(export, () => rebuilds++);

        try
        {
            TInterface.TExportModificationApply(export, failures.Add);

            Assert.Equal(1, rebuilds);
            Assert.Empty(failures);

            rebuilds = 0;
            LPresetSelection.LPresetSaveSeam = (_, _) => false;

            TInterface.TExportModificationApply(export, failures.Add);

            Assert.Equal(0, rebuilds);
            Assert.Equal(["Alpha"], failures);
        }
        finally
        {
            LPresetSelection.LPresetSaveSeam = save;
            LPresetPersistence.LPresetSaveSeam = persist;
        }
    }

    [Fact]
    public void ModificationRestore_PublishesOneRepaint()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        QExport export = TInterface.TExportCreate(selection, "Convert");
        TInterface.TExportOwnerAttach(export);
        int rebuilds = 0;
        TInterface.TExportAttach(export, () => rebuilds++);

        TInterface.TExportModificationRestore(export);

        Assert.Equal(1, rebuilds);
    }
    [Fact]
    public void Cleanup_RestoresIconCache() {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() => {
            try
            {
                using TPreset presets = new();
                presets.TPresetSeedCreate("Alpha");
                LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
                QExport export = TInterface.TExportCreate(selection, "Convert");
                (PExport surface, object[] state) = TInterface.TExportSurfaceRead(export);
                TInterface.TInterfaceExportRead(surface, false, state);
                TInterface.TPresetSelectionClose(selection);
                Assert.Equal(0, TInterface.TInterfaceExportRead(state, 0));
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
