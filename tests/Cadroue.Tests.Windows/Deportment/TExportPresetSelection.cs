using Cadroue.Application;
using Cadroue.UIDeportment;
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
        Assert.True(export.QExportDragActive);

        TInterface.TExportSync(export);
        Assert.Equal(0, rebuilds);

        Assert.True(TInterface.TExportDragClear(export));
        Assert.Null(export.QExportDragging);
        Assert.Equal(1, rebuilds);
        Assert.False(TInterface.TExportDragClear(export));
    }
}
