using System.IO;
using System.Text.Json;

namespace Cadroue.UIShell.PPanels;

internal static class LExportSpecificPresetStore
{
    private const string LPresetFolderName = "Cadroue";
    private const string LPresetFileName = "LExportSpecificPresets.json";

    internal static IReadOnlyList<LExportSpecificState>? LPresetLoad()
    {
        string lPresetPath = LPresetPathCreate();
        if (!File.Exists(lPresetPath))
        {
            return null;
        }

        try
        {
            string lPresetJson = File.ReadAllText(lPresetPath);
            var lRecords = JsonSerializer.Deserialize<List<LExportSpecificPresetRecord>>(lPresetJson) ?? new();
            return lRecords.Select(lRecord => lRecord.LPresetStateCreate()).ToList();
        }
        catch
        {
            return null;
        }
    }

    internal static void LPresetSave(IReadOnlyList<LExportSpecificState> lPresets)
    {
        string lPresetPath = LPresetPathCreate();
        string? lPresetFolder = Path.GetDirectoryName(lPresetPath);
        if (!string.IsNullOrWhiteSpace(lPresetFolder))
        {
            Directory.CreateDirectory(lPresetFolder);
        }

        var lRecords = lPresets.Select(LExportSpecificPresetRecord.LPresetRecordCreate).ToList();
        string lPresetJson = JsonSerializer.Serialize(lRecords, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lPresetPath, lPresetJson);
    }

    internal static void LPresetFileSave(LExportSpecificState lPreset, string lPresetFilePath)
    {
        var lRecord = LExportSpecificPresetRecord.LPresetRecordCreate(lPreset);
        string lPresetJson = JsonSerializer.Serialize(lRecord, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lPresetFilePath, lPresetJson);
    }

    internal static LExportSpecificState? LPresetFileLoad(string lPresetFilePath)
    {
        string lPresetJson = File.ReadAllText(lPresetFilePath);
        var lRecord = JsonSerializer.Deserialize<LExportSpecificPresetRecord>(lPresetJson);
        return lRecord?.LPresetStateCreate();
    }

    private static string LPresetPathCreate()
    {
        string lAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lAppData, LPresetFolderName, LPresetFileName);
    }
}

public sealed class LExportSpecificPresetRecord
{
    public string PresetName { get; set; } = "MP4_H264_AAC_Default";
    public string Name { get; set; } = "OriginalName_export";
    public string Container { get; set; } = "MP4";
    public string ExportMode { get; set; } = "Smart export";
    public string VideoStream { get; set; } = "Include";
    public string AudioStream { get; set; } = "Include first audio track";
    public string VideoMode { get; set; } = "Auto";
    public string AudioMode { get; set; } = "Auto";

    public string VideoEncoder { get; set; } = "H.264, x264 / libx264";
    public string VideoRateControl { get; set; } = "CRF (constant quality)";
    public string VideoQuality { get; set; } = "23";
    public string VideoSpeedPreset { get; set; } = "medium";
    public string Location { get; set; } = "Same as source";
    public string LocationFolder { get; set; } = string.Empty;
    public string VideoSize { get; set; } = "Same as source";
    public bool VideoSizeReactive { get; set; }
    public string VideoFps { get; set; } = "Same as source";
    public string PixelFormat { get; set; } = "Auto";
    public Dictionary<string, string> VideoExtras { get; set; } = new();
    public string AudioEncoder { get; set; } = "AAC";
    public string AudioBitrate { get; set; } = "96k";
    public string AudioSampleRate { get; set; } = "Same as source";
    public string AudioChannels { get; set; } = "Same as source";

    public static LExportSpecificPresetRecord LPresetRecordCreate(LExportSpecificState lState) => new()
    {
        PresetName = lState.PresetName,
        Name = lState.Name,
        Container = lState.Container,
        ExportMode = lState.ExportMode,
        VideoStream = lState.VideoStream,
        AudioStream = lState.AudioStream,
        VideoMode = lState.VideoMode,
        AudioMode = lState.AudioMode,
        VideoEncoder = lState.VideoEncoder,
        VideoRateControl = lState.VideoRateControl,
        VideoQuality = lState.VideoQuality,
        VideoSpeedPreset = lState.VideoSpeedPreset,
        Location = lState.Location,
        LocationFolder = lState.LocationFolder,
        VideoSize = lState.VideoSize,
        VideoSizeReactive = lState.VideoSizeReactive,
        VideoFps = lState.VideoFps,
        PixelFormat = lState.PixelFormat,
        VideoExtras = new Dictionary<string, string>(lState.VideoExtras),
        AudioEncoder = lState.AudioEncoder,
        AudioBitrate = lState.AudioBitrate,
        AudioSampleRate = lState.AudioSampleRate,
        AudioChannels = lState.AudioChannels
    };

    public LExportSpecificState LPresetStateCreate() => new()
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
        VideoSizeReactive = VideoSizeReactive,
        VideoFps = VideoFps,
        PixelFormat = PixelFormat,
        VideoExtras = new Dictionary<string, string>(VideoExtras, StringComparer.Ordinal),
        AudioEncoder = AudioEncoder,
        AudioBitrate = AudioBitrate,
        AudioSampleRate = AudioSampleRate,
        AudioChannels = AudioChannels
    };
}
