using Cadroue.Application;

namespace Cadroue.Infrastructure;

public static class LRigFactoryPreset
{
    public static void LRigPresetAttach()
    {
        LPreset.LPresetTraceSeam = lPresetMessage => LTraceLog.LTraceWarningRecord(lPresetMessage);
        LPreset.LPresetNativeSeam = LPresetStore.LPresetNativeLoad;
        LPreset.LPresetLoadSeam = LPresetStore.LPresetLoad;
        LPreset.LPresetSaveSeam = LPresetStore.LPresetSave;
        LPresetSelection.LPresetLoadSeam = LPreset.LPresetSelectionRead;
        LPresetSelection.LPresetSaveSeam = LPreset.LPresetSelectionSave;
        LPresetSelection.LPresetRenameSeam = LPreset.LPresetSelectionSet;
        LPresetSelection.LPresetOutputSeam = LPreset.LPresetSelectionResolve;
    }
}
