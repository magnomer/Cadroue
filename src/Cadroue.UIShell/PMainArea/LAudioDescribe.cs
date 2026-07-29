using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LAudio
{
    public static int LAudioDescribe(
        LWorkPriority lWorkPriority,
        string? lAudioSourcePath,
        LWorkAudio lAudioProcessing,
        LExportSpecificState lExportSpecificState)
    {
        return LAudio.LAudioInterpret(
            lWorkPriority,
            lAudioSourcePath,
            lAudioProcessing,
            lExportSpecificState.LPresetOutputCreate());
    }

    public static int LAudioAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lAudioSourcePaths,
        LWorkAudio lAudioProcessing,
        LExportSpecificState lExportSpecificState)
    {
        LWorkOutput lAudioOutput = lExportSpecificState.LPresetOutputCreate();
        int lAudioAdded = 0;
        foreach (string lAudioSourcePath in lAudioSourcePaths)
        {
            lAudioAdded += LAudio.LAudioInterpret(lWorkPriority, lAudioSourcePath, lAudioProcessing, lAudioOutput);
        }

        return lAudioAdded;
    }
}
