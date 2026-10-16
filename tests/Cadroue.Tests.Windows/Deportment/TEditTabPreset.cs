using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TEditTabPreset
{
    [Fact]
    public void Run_MissingPreset_RaisesMissingOnce()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate();
        QEditTab tab = TInterface.TEditTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"), TInterfaceViewer.TViewerCreate(), TInterfaceList.TListCreate());
        int missing = 0;
        TInterface.TEditMissingAttach(tab, () => missing++);

        TInterface.TEditRun(tab, LWorkPriority.LWorkPriorityNormal);

        Assert.Equal(1, missing);
    }
}
