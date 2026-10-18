using Cadroue.Application;

namespace Cadroue.Infrastructure;

public static class LRigFactoryPreset
{
    public static void LRigPresetAttach()
    {
        LPresetPersistence.LPresetTraceSeam = lPresetMessage => LTraceLog.LTraceWarningRecord(lPresetMessage);
        LPresetPersistence.LPresetNativeSeam = LPresetNative.LPresetNativeLoad;
        LPresetPersistence.LPresetLoadSeam = LPresetStore.LPresetLoad;
        LPresetPersistence.LPresetSaveSeam = LPresetStore.LPresetSave;
        LPresetDraft.LPresetLoadSeam = LPresetTable.LPresetSelectionRead;
        LPresetSelection.LPresetSaveSeam = LPresetTable.LPresetSelectionSave;
        LPresetSelection.LPresetRenameSeam = LPresetTable.LPresetSelectionSet;
        LPresetSelection.LPresetOutputSeam = LPreset.LPresetSelectionResolve;
    }
}
