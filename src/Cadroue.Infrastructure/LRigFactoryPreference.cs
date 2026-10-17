using Cadroue.Application;

namespace Cadroue.Infrastructure;

public static class LRigFactoryPreference
{
    public static void LRigPreferenceAttach()
    {
        LPreference.LPreferenceLanguageSeam = LLocalization.LLocalizationLanguageNormalize;
        LPreference.LPreferenceLoadSeam = LPreferenceStateStore.LPreferenceStateLoad;
        LPreference.LPreferenceSaveSeam = LPreferenceStateStore.LPreferenceStateSave;
        LPreference.LPreferenceTraceSeam = LTraceLog.LTraceInfoRecord;
        LRendererFolder.LRendererFolderSeam = () => LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder;
        LRenderer.LRendererEngineSeam = () => LPreference.LPreferenceStateCurrent.LPreferencePreviewEngine;
        LRendererError.LRendererTextSeam = LLocalization.LLocalizationTextRead;
        LSectionPalette.LSectionPaletteSeam = () => LPreference.LPreferenceStateCurrent.LPreferenceSectionPalette;
    }
}
