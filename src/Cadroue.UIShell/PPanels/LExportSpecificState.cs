using System.Collections.ObjectModel;

namespace Cadroue.UIShell.PPanels;

public sealed class LExportSpecificState
{
    private static readonly Dictionary<string, LExportSpecificState> LPresetMap = new(StringComparer.OrdinalIgnoreCase);

    static LExportSpecificState()
    {
        IReadOnlyList<LExportSpecificState>? lStoredPresets = LExportSpecificPresetStore.LPresetLoad();
        if (lStoredPresets is null)
        {
            var lDefault = new LExportSpecificState { PresetName = "MP4_H264_AAC_Default" };
            LPresetMap[lDefault.PresetName] = lDefault.LClone();
            LPresetNames.Add(lDefault.PresetName);
            return;
        }

        foreach (LExportSpecificState lPreset in lStoredPresets)
        {
            if (string.IsNullOrWhiteSpace(lPreset.PresetName))
            {
                continue;
            }

            string lName = lPreset.PresetName.Trim();
            lPreset.PresetName = lName;
            LPresetMap[lName] = lPreset.LClone();
            LPresetNames.Add(lName);
        }
    }

    public static ObservableCollection<string> LPresetNames { get; } = new();

    public string PresetName { get; set; } = "MP4_H264_AAC_Default";
    public string Name { get; set; } = "{OriginalName}_export";
    public string Container { get; set; } = "MP4";
    public string ExportMode { get; set; } = "Smart export";
    public string VideoStream { get; set; } = "Include";
    public string AudioStream { get; set; } = "Include first audio track";
    public string VideoMode { get; set; } = "Auto";
    public string AudioMode { get; set; } = "Auto";

    public string VideoSummary => $"{VideoMode} ({VideoStream})";
    public string AudioSummary => $"{AudioMode} ({LAudioStreamSummary})";
    public string OutputSummary => string.IsNullOrWhiteSpace(LContainerExtension) ? Name : $"{Name}.{LContainerExtension}";

    public LExportSpecificState LClone() => new()
    {
        PresetName = PresetName,
        Name = Name,
        Container = Container,
        ExportMode = ExportMode,
        VideoStream = VideoStream,
        AudioStream = AudioStream,
        VideoMode = VideoMode,
        AudioMode = AudioMode
    };

    public void LCopyFrom(LExportSpecificState lSource)
    {
        PresetName = lSource.PresetName;
        Name = lSource.Name;
        Container = lSource.Container;
        ExportMode = lSource.ExportMode;
        VideoStream = lSource.VideoStream;
        AudioStream = lSource.AudioStream;
        VideoMode = lSource.VideoMode;
        AudioMode = lSource.AudioMode;
    }

    public static bool LPresetTryLoad(string lPresetName, LExportSpecificState lTarget)
    {
        if (!LPresetMap.TryGetValue(lPresetName, out var lPreset))
        {
            return false;
        }

        lTarget.LCopyFrom(lPreset);
        lTarget.PresetName = lPresetName;
        return true;
    }

    public static void LPresetSave(string lPresetName, LExportSpecificState lSource)
    {
        if (string.IsNullOrWhiteSpace(lPresetName))
        {
            return;
        }

        string lName = lPresetName.Trim();
        var lPreset = lSource.LClone();
        lPreset.PresetName = lName;
        LPresetMap[lName] = lPreset;
        if (!LPresetNames.Any(lExisting => string.Equals(lExisting, lName, StringComparison.OrdinalIgnoreCase)))
        {
            LPresetNames.Add(lName);
        }
        LPresetPersist();
    }

    public static bool LPresetDelete(string lPresetName)
    {
        if (string.IsNullOrWhiteSpace(lPresetName))
        {
            return false;
        }

        string lName = lPresetName.Trim();
        if (!LPresetMap.Remove(lName))
        {
            return false;
        }

        for (int lIndex = LPresetNames.Count - 1; lIndex >= 0; lIndex--)
        {
            if (string.Equals(LPresetNames[lIndex], lName, StringComparison.OrdinalIgnoreCase))
            {
                LPresetNames.RemoveAt(lIndex);
            }
        }
        LPresetPersist();
        return true;
    }

    public static string? LPresetFirstName => LPresetNames.Count > 0 ? LPresetNames[0] : null;

    private static void LPresetPersist()
    {
        var lPresets = new List<LExportSpecificState>();
        foreach (string lName in LPresetNames)
        {
            if (LPresetMap.TryGetValue(lName, out LExportSpecificState? lPreset))
            {
                lPresets.Add(lPreset.LClone());
            }
        }
        LExportSpecificPresetStore.LPresetSave(lPresets);
    }

    private string LAudioStreamSummary => AudioStream switch
    {
        "Include first audio track" => "Include the first track",
        "Include all audio tracks" => "Include all tracks",
        _ => AudioStream
    };

    private string LContainerExtension => Container switch
    {
        "MP4" => "mp4",
        "Matroska" => "mkv",
        "MOV" => "mov",
        "WebM" => "webm",
        "M4A" => "m4a",
        "MP3" => "mp3",
        "WAV" => "wav",
        "FLAC" => "flac",
        "OGG" => "ogg",
        _ => ""
    };
}
