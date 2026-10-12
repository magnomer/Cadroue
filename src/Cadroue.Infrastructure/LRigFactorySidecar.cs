using Cadroue.Application;

namespace Cadroue.Infrastructure;

public static class LRigFactorySidecar
{
    public static void LRigSidecarAttach()
    {
        LLibrarian.LLibrarianCoreReader = LSidecarStore.LSidecarCoreRead;
        LLibrarian.LLibrarianKeyframesSeam = LSidecarStore.LSidecarKeyframesRead;
        LLibrarian.LLibrarianWaveformReader = LSidecarStore.LSidecarWaveformRead;
        LLibrarian.LLibrarianEditReader = LSidecarStore.LSidecarEditRead;
        LLibrarian.LLibrarianAudioReader = LSidecarStore.LSidecarAudioRead;
        LLibrarian.LLibrarianSplitReader = LSidecarStore.LSidecarSplitRead;
        LLibrarian.LLibrarianFixReader = LSidecarStore.LSidecarFixRead;
        LLibrarian.LLibrarianDiagnosisReader = LSidecarStore.LSidecarDiagnosisRead;
        LLibrarian.LLibrarianLoudnessReader = LSidecarStore.LSidecarLoudnessRead;
        LLibrarian.LLibrarianDurationReader = LSidecarStore.LSidecarDurationRead;
        LLibrarian.LLibrarianDurationResolver = LSidecarStore.LSidecarDurationResolve;
        LLibrarian.LLibrarianEditWriter = LSidecarStore.LSidecarEditSave;
        LLibrarian.LLibrarianAudioWriter = LSidecarStore.LSidecarAudioSave;
        LLibrarian.LLibrarianSplitWriter = LSidecarStore.LSidecarSplitSave;
        LLibrarian.LLibrarianFixWriter = LSidecarStore.LSidecarFixSave;
        LLibrarian.LLibrarianDiagnosisWriter = LSidecarStore.LSidecarDiagnosisSave;
        LLibrarian.LLibrarianLoudnessWriter = LSidecarStore.LSidecarLoudnessSave;
        LLibrarian.LLibrarianWaveformWriter = LSidecarStore.LSidecarWaveformSave;
        LLibrarian.LLibrarianFileChecker = LSidecarStore.LSidecarFileCheck;
        LLibrarian.LLibrarianSourceResolver = LSidecarStore.LSidecarSourceResolve;
        LLibrarian.LLibrarianSourceMatcher = LSidecarStore.LSidecarSourceMatch;
        LSegment.LSegmentLoadSeam = LSidecarStore.LSidecarSectionsRead;
        LSegment.LSegmentSaveSeam = LSidecarStore.LSidecarSectionsSave;
        LSidecarStore.LSidecarCoreSeam = LLibrarian.LLibrarianLoad;
    }
}
