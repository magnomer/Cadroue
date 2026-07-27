using System.Collections.ObjectModel;
using Cadroue.Core;

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
            LPresetMap[lDefault.PresetName] = lDefault.LPresetClone();
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
            LPresetMap[lName] = lPreset.LPresetClone();
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

    // Encoder rate control. Kept as display text so it round-trips through the
    // dialog combos; the FFmpeg option behind each value lives in LCapabilityTable.
    public string VideoEncoder { get; set; } = "H.264, x264 / libx264";
    public string VideoRateControl { get; set; } = "CRF (constant quality)";
    public string VideoQuality { get; set; } = "23";
    public string VideoSpeedPreset { get; set; } = "medium";

    // Destination. "Same as source" leaves LocationFolder empty and resolves against
    // the source file's own folder at schedule time; "Custom folder" requires one.
    public string Location { get; set; } = "Same as source";
    public string LocationFolder { get; set; } = string.Empty;

    public string VideoSize { get; set; } = "Same as source";
    public string VideoFps { get; set; } = "Same as source";
    public string PixelFormat { get; set; } = "Auto";

    // Per-encoder extra options (tune, usage, profile, deadline, scenario, WebP content
    // preset...), keyed by FFmpeg option. The key set changes with the encoder, so this
    // cannot be a fixed list of properties.
    public Dictionary<string, string> VideoExtras { get; set; } = new(StringComparer.Ordinal);

    public string AudioEncoder { get; set; } = "AAC";
    public string AudioBitrate { get; set; } = "96k";
    public string AudioSampleRate { get; set; } = "Same as source";
    public string AudioChannels { get; set; } = "Same as source";

    public string VideoSummary => $"{VideoMode} ({VideoStream})";
    public string AudioSummary => $"{AudioMode} ({LAudioStreamSummary})";
    public string OutputSummary => string.IsNullOrWhiteSpace(LContainerExtension) ? Name : $"{Name}.{LContainerExtension}";

    /// <summary>
    /// Take a UI-free snapshot for the backend schedule. Called when work is queued so
    /// the queued item keeps these settings even if the panel changes afterwards.
    /// </summary>
    public LWorkOutput LPresetOutputCreate() => new(
        Name,
        Container,
        LContainerExtension,
        Location,
        LocationFolder,
        ExportMode,
        VideoStream,
        VideoMode,
        VideoEncoder,
        VideoRateControl,
        VideoQuality,
        VideoSpeedPreset,
        VideoSize,
        VideoFps,
        PixelFormat,
        new Dictionary<string, string>(VideoExtras, StringComparer.Ordinal),
        AudioStream,
        AudioMode,
        AudioEncoder,
        AudioBitrate,
        AudioSampleRate,
        AudioChannels);

    public LExportSpecificState LPresetClone() => new()
    {
        PresetName = PresetName,
        Name = Name,
        Container = Container,
        ExportMode = ExportMode,
        VideoStream = VideoStream,
        AudioStream = AudioStream,
        VideoMode = VideoMode,
        AudioMode = AudioMode,
        VideoEncoder = VideoEncoder,
        VideoRateControl = VideoRateControl,
        VideoQuality = VideoQuality,
        VideoSpeedPreset = VideoSpeedPreset,
        Location = Location,
        LocationFolder = LocationFolder,
        VideoSize = VideoSize,
        VideoFps = VideoFps,
        PixelFormat = PixelFormat,
        VideoExtras = new Dictionary<string, string>(VideoExtras, StringComparer.Ordinal),
        AudioEncoder = AudioEncoder,
        AudioBitrate = AudioBitrate,
        AudioSampleRate = AudioSampleRate,
        AudioChannels = AudioChannels
    };

    public void LPresetCopy(LExportSpecificState lSource)
    {
        PresetName = lSource.PresetName;
        Name = lSource.Name;
        Container = lSource.Container;
        ExportMode = lSource.ExportMode;
        VideoStream = lSource.VideoStream;
        AudioStream = lSource.AudioStream;
        VideoMode = lSource.VideoMode;
        AudioMode = lSource.AudioMode;
        VideoEncoder = lSource.VideoEncoder;
        VideoRateControl = lSource.VideoRateControl;
        VideoQuality = lSource.VideoQuality;
        VideoSpeedPreset = lSource.VideoSpeedPreset;
        Location = lSource.Location;
        LocationFolder = lSource.LocationFolder;
        VideoSize = lSource.VideoSize;
        VideoFps = lSource.VideoFps;
        PixelFormat = lSource.PixelFormat;
        VideoExtras = new Dictionary<string, string>(lSource.VideoExtras, StringComparer.Ordinal);
        AudioEncoder = lSource.AudioEncoder;
        AudioBitrate = lSource.AudioBitrate;
        AudioSampleRate = lSource.AudioSampleRate;
        AudioChannels = lSource.AudioChannels;
    }

    public static bool LPresetTryLoad(string lPresetName, LExportSpecificState lTarget)
    {
        if (!LPresetMap.TryGetValue(lPresetName, out var lPreset))
        {
            return false;
        }

        lTarget.LPresetCopy(lPreset);
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
        var lPreset = lSource.LPresetClone();
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
                lPresets.Add(lPreset.LPresetClone());
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
