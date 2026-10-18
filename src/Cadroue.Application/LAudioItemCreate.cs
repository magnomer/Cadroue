using Cadroue.Core;

namespace Cadroue.Application;

public static class LAudio
{
    public static LWorkItem? LAudioItemCreate(
        LWorkPriority lWorkPriority,
        string? lAudioSourcePath,
        LWorkAudio lAudioProcessing,
        LEncoding lAudioOutput,
        string lAudioTab,
        Action<string> lInfoLog,
        Action<string> lErrorLog,
        Func<string, TimeSpan> lAudioDurationRead,
        Guid lAudioBatchId = default)
    {
        if (string.IsNullOrWhiteSpace(lAudioSourcePath))
        {
            lErrorLog("Audio job not queued: no source file is open");
            return null;
        }

        if (!lAudioOutput.LEncodingSupportCheck(LWorkKind.LWorkKindAudio))
        {
            lErrorLog(
                "Audio job not queued: the export preset does not encode the audio stream " +
                $"(mode '{lAudioOutput.LEncodingAudio.LEncodingMode}', " +
                $"scope '{lAudioOutput.LEncodingAudio.LEncodingStream}')");
            return null;
        }

        TimeSpan lAudioDuration = lAudioDurationRead(lAudioSourcePath);
        string lAudioFolder = LEncodingPath.LEncodingFolderRead(lAudioOutput, lAudioSourcePath);
        string lAudioOutputName = LAudioName.LAudioNameCreate(lAudioSourcePath, lAudioFolder, lAudioOutput);
        Guid lAudioBatch = lAudioBatchId != Guid.Empty ? lAudioBatchId : LGate.LGateBatchCreate();

        lInfoLog(
            $"Audio built job at {lWorkPriority} from '{Path.GetFileName(lAudioSourcePath)}' " +
            $"into '{lAudioFolder}' as '{lAudioOutputName}'");

        return new LWorkItem(
            lAudioBatch,
            LWorkKind.LWorkKindAudio,
            lWorkPriority,
            lAudioSourcePath,
            TimeSpan.Zero,
            lAudioDuration,
            lAudioOutputName,
            Path.Combine(lAudioFolder, lAudioOutputName),
            lAudioOutput,
            lWorkAudio: lAudioProcessing.LWorkAudioSkip ? LWorkAudio.LWorkAudioCreate() : lAudioProcessing)
        {
            LWorkTab = lAudioTab
        };
    }
}
