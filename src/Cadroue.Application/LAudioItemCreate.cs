using System.IO;
using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LAudio
{
    public static LWorkItem? LAudioItemCreate(
        LWorkPriority lWorkPriority,
        string? lAudioSourcePath,
        LWorkAudio lAudioProcessing,
        LEncoding lAudioOutput,
        string lAudioTab,
        Action<string> lInfoLog,
        Action<string> lErrorLog,
        Guid lAudioBatchId = default)
    {
        if (string.IsNullOrWhiteSpace(lAudioSourcePath))
        {
            lErrorLog("Audio job not queued: no source file is open");
            return null;
        }

        string lAudioFolder = lAudioOutput.LEncodingFolderRead(lAudioSourcePath);
        string lAudioOutputName = LAudioNameCreate(lAudioSourcePath, lAudioFolder, lAudioOutput);
        Guid lAudioBatch = lAudioBatchId != Guid.Empty ? lAudioBatchId : Guid.NewGuid();

        lInfoLog(
            $"Audio built job at {lWorkPriority} from '{Path.GetFileName(lAudioSourcePath)}' " +
            $"into '{lAudioFolder}' as '{lAudioOutputName}'");

        return new LWorkItem(
            lAudioBatch,
            LWorkKind.LWorkKindAudio,
            lWorkPriority,
            lAudioSourcePath,
            TimeSpan.Zero,
            TimeSpan.Zero,
            lAudioOutputName,
            Path.Combine(lAudioFolder, lAudioOutputName),
            lAudioOutput,
            lWorkAudio: lAudioProcessing.LWorkAudioSkip ? LWorkAudio.LWorkAudioCreate() : lAudioProcessing)
        {
            LWorkTab = lAudioTab
        };
    }

    private static string LAudioNameCreate(string lAudioSourcePath, string lAudioFolder, LEncoding lAudioOutput)
    {
        string lAudioStem = Path.GetFileNameWithoutExtension(lAudioSourcePath);
        string lAudioExtension = lAudioOutput.LEncodingExtensionResolve(lAudioSourcePath);
        string lAudioBaseName = string.IsNullOrWhiteSpace(lAudioExtension)
            ? lAudioStem
            : $"{lAudioStem}.{lAudioExtension}";

        string lAudioCandidate = Path.Combine(lAudioFolder, lAudioBaseName);
        if (!string.Equals(
                Path.GetFullPath(lAudioCandidate),
                Path.GetFullPath(lAudioSourcePath),
                StringComparison.OrdinalIgnoreCase))
        {
            return lAudioBaseName;
        }

        return string.IsNullOrWhiteSpace(lAudioExtension)
            ? $"{lAudioStem}_audio"
            : $"{lAudioStem}_audio.{lAudioExtension}";
    }
}
