using Cadroue.Application;

namespace Cadroue.Infrastructure;

public static class LRigFactorySidecar
{
    public static void LRigSidecarAttach()
    {
        LLibrarian.LLibrarianCoreReader = LSidecarField.LSidecarCoreRead;
        LLibrarian.LLibrarianKeyframesSeam = LSidecarStore.LSidecarKeyframesRead;
        LLibrarian.LLibrarianWaveformReader = LSidecarField.LSidecarWaveformRead;
        LLibrarian.LLibrarianEditReader = LSidecarField.LSidecarEditRead;
        LLibrarian.LLibrarianAudioReader = LSidecarField.LSidecarAudioRead;
        LLibrarian.LLibrarianSplitReader = LSidecarField.LSidecarSplitRead;
        LLibrarian.LLibrarianFixReader = LSidecarField.LSidecarFixRead;
        LLibrarian.LLibrarianDiagnosisReader = LSidecarField.LSidecarDiagnosisRead;
        LLibrarian.LLibrarianLoudnessReader = LSidecarField.LSidecarLoudnessRead;
        LLibrarian.LLibrarianDurationReader = LSidecarDuration.LSidecarDurationRead;
        LLibrarian.LLibrarianDurationResolver = LSidecarDuration.LSidecarDurationResolve;
        LLibrarian.LLibrarianEditWriter = LSidecarField.LSidecarEditSave;
        LLibrarian.LLibrarianAudioWriter = LSidecarField.LSidecarAudioSave;
        LLibrarian.LLibrarianSplitWriter = LSidecarField.LSidecarSplitSave;
        LLibrarian.LLibrarianFixWriter = LSidecarField.LSidecarFixSave;
        LLibrarian.LLibrarianDiagnosisWriter = LSidecarField.LSidecarDiagnosisSave;
        LLibrarian.LLibrarianLoudnessWriter = LSidecarField.LSidecarLoudnessSave;
        LLibrarian.LLibrarianWaveformWriter = LSidecarField.LSidecarWaveformSave;
        LLibrarian.LLibrarianFileChecker = LSidecarPath.LSidecarFileCheck;
        LLibrarian.LLibrarianSourceResolver = LSidecarStore.LSidecarSourceResolve;
        LLibrarian.LLibrarianSourceMatcher = LSidecarStore.LSidecarSourceMatch;
        LSegment.LSegmentLoadSeam = LSidecarField.LSidecarSectionsRead;
        LSegment.LSegmentSaveSeam = LSidecarField.LSidecarSectionsSave;
        LSidecarField.LSidecarCoreSeam = LLibrarian.LLibrarianLoad;
    }
}
