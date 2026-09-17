using Cadroue.Application;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TExportPresetSelect
{
    [Fact]
    public void Select_ChangesOwner_RebuildsRows()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha", "Beta");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        LExport export = TInterface.TExportCreate(selection, true);
        TInterface.TExportOwnerAttach(export);
        int rebuilds = 0;
        TInterface.TExportAttach(export, () => rebuilds++);

        TInterface.TExportSelect(export, "Beta");

        Assert.Equal("Beta", export.LExportSelected);
        Assert.Equal("Beta", selection.LPresetSelectionName);
        Assert.True(export.LExportSmartAllowed);
        Assert.Equal(1, rebuilds);
    }

    [Fact]
    public void EditStart_SelectsAndMarksEditing_CancelClears()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha", "Beta");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        LExport export = TInterface.TExportCreate(selection, false);
        TInterface.TExportOwnerAttach(export);
        int rebuilds = 0;
        TInterface.TExportAttach(export, () => rebuilds++);

        TInterface.TExportEditStart(export, "Beta");

        Assert.Equal("Beta", export.LExportSelected);
        Assert.Equal("Beta", export.LExportEditing);
        Assert.Equal(2, rebuilds);

        TInterface.TExportSync(export);
        Assert.Equal(2, rebuilds);

        TInterface.TExportEditCancel(export);

        Assert.Null(export.LExportEditing);
        Assert.Equal(3, rebuilds);
    }

    [Fact]
    public void NameCommit_OnlyForEditedRule_RenamesSelection()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha", "Beta");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        LExport export = TInterface.TExportCreate(selection, false);
        TInterface.TExportEditStart(export, "Alpha");

        Assert.False(TInterface.TExportNameCommit(export, "Beta", "Gamma"));
        Assert.True(TInterface.TExportNameCommit(export, "Alpha", "Gamma"));

        Assert.Null(export.LExportEditing);
        Assert.Equal("Gamma", selection.LPresetSelectionName);
        Assert.Equal("Gamma", export.LExportSelected);
    }

    [Fact]
    public void Drag_DefersSync_UntilCleared()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha", "Beta");
        LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
        LExport export = TInterface.TExportCreate(selection, false);
        int rebuilds = 0;
        TInterface.TExportAttach(export, () => rebuilds++);

        Assert.False(TInterface.TExportDragMove(export));
        TInterface.TExportDragStart(export, "Alpha");
        Assert.True(TInterface.TExportDragMove(export));
        Assert.True(export.LExportDragActive);

        TInterface.TExportSync(export);
        Assert.Equal(0, rebuilds);

        Assert.True(TInterface.TExportDragClear(export));
        Assert.Null(export.LExportDragging);
        Assert.Equal(1, rebuilds);
        Assert.False(TInterface.TExportDragClear(export));
    }
}
