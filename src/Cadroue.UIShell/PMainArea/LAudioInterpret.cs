using System.IO;
using Cadroue.Core;

namespace Cadroue.UIShell.PMainArea;

public static partial class LAudio
{
    public static int LAudioInterpret(
        LWorkPriority lWorkPriority,
        string? lAudioSourcePath,
        LWorkAudio lAudioProcessing,
        LWorkOutput lAudioOutput)
    {
        if (string.IsNullOrWhiteSpace(lAudioSourcePath))
        {
            LAppLog.LError("Audio job not queued: no source file is open");
            return 0;
        }

        string lAudioFolder = lAudioOutput.LWorkFolderRead(lAudioSourcePath);
        string lAudioOutputName = LAudioNameCreate(lAudioSourcePath, lAudioFolder, lAudioOutput);
        Guid lAudioBatchId = Guid.NewGuid();

        var lAudioItem = new LWorkItem(
            lAudioBatchId,
            LWorkKind.LWorkKindAudio,
            lWorkPriority,
            lAudioSourcePath,
            TimeSpan.Zero,
            TimeSpan.Zero,
            lAudioOutputName,
            Path.Combine(lAudioFolder, lAudioOutputName),
            lAudioOutput,
            lWorkAudio: lAudioProcessing);

        int lAudioAdded = LSchedule.LScheduleCurrent.LScheduleAdd(new[] { lAudioItem });
        LAppLog.LInfo(
            $"Audio queued {lAudioAdded} job at {lWorkPriority} from '{Path.GetFileName(lAudioSourcePath)}' " +
            $"into '{lAudioFolder}' as '{lAudioOutputName}'");
        return lAudioAdded;
    }

    private static string LAudioNameCreate(string lAudioSourcePath, string lAudioFolder, LWorkOutput lAudioOutput)
    {
        string lAudioStem = Path.GetFileNameWithoutExtension(lAudioSourcePath);
        string lAudioExtension = lAudioOutput.LWorkOutputExtension.TrimStart('.');
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
