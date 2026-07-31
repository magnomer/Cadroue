using System.IO;
using System.Text.Json;

namespace Cadroue.UIShell.PPanels;

internal static class LPresetStore
{
    private const string LPresetFolderName = "Cadroue";
    private const string LPresetFileName = "LExportSpecificPresets.json";

    internal static IReadOnlyList<LPreset>? LPresetLoad()
    {
        string lPresetPath = LPresetPathCreate();
        if (!File.Exists(lPresetPath))
        {
            return null;
        }

        try
        {
            string lPresetJson = File.ReadAllText(lPresetPath);
            var lRecords = JsonSerializer.Deserialize<List<LPresetRecord>>(lPresetJson) ?? new();
            return lRecords.Select(lRecord => lRecord.LPresetStateCreate()).ToList();
        }
        catch
        {
            return null;
        }
    }

    internal static void LPresetSave(IReadOnlyList<LPreset> lPresets)
    {
        string lPresetPath = LPresetPathCreate();
        string? lPresetFolder = Path.GetDirectoryName(lPresetPath);
        if (!string.IsNullOrWhiteSpace(lPresetFolder))
        {
            Directory.CreateDirectory(lPresetFolder);
        }

        var lRecords = lPresets.Select(LPresetRecord.LPresetRecordCreate).ToList();
        string lPresetJson = JsonSerializer.Serialize(lRecords, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lPresetPath, lPresetJson);
    }

    internal static void LPresetFileSave(LPreset lPreset, string lPresetFilePath)
    {
        var lRecord = LPresetRecord.LPresetRecordCreate(lPreset);
        string lPresetJson = JsonSerializer.Serialize(lRecord, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(lPresetFilePath, lPresetJson);
    }

    internal static LPreset? LPresetFileLoad(string lPresetFilePath)
    {
        string lPresetJson = File.ReadAllText(lPresetFilePath);
        var lRecord = JsonSerializer.Deserialize<LPresetRecord>(lPresetJson);
        return lRecord?.LPresetStateCreate();
    }

    private static string LPresetPathCreate()
    {
        string lAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lAppData, LPresetFolderName, LPresetFileName);
    }
}

public sealed class LPresetRecord
{
    public string LPresetName { get; set; } = "MP4_H264_AAC_Default";
    public string LPresetDisplay { get; set; } = "OriginalName_export";
    public string LPresetContainer { get; set; } = "MP4";
    public string LPresetExtension { get; set; } = string.Empty;
    public string LPresetExportMode { get; set; } = "Smart export";
    public string LPresetVideoStream { get; set; } = "Include";
    public string LPresetAudioStream { get; set; } = "Include first audio track";
    public string LPresetVideoMode { get; set; } = "Auto";
    public string LPresetAudioMode { get; set; } = "Auto";

    public string LPresetVideoEncoder { get; set; } = "H.264, x264 / libx264";
    public string LPresetRateControl { get; set; } = "CRF (constant quality)";
    public string LPresetVideoQuality { get; set; } = "23";
    public string LPresetSpeedPreset { get; set; } = "medium";
    public string LPresetLocation { get; set; } = "Same as source";
    public string LPresetLocationFolder { get; set; } = string.Empty;
    public string LPresetVideoSize { get; set; } = "Same as source";
    public bool LPresetSizeReactive { get; set; }
    public string LPresetVideoFps { get; set; } = "Same as source";
    public string LPresetPixelLayout { get; set; } = "Auto";
    public Dictionary<string, string> LPresetVideoExtras { get; set; } = new();
    public string LPresetAudioEncoder { get; set; } = "AAC";
    public string LPresetAudioBitrate { get; set; } = "96k";
    public string LPresetSampleRate { get; set; } = "Same as source";
    public string LPresetAudioChannels { get; set; } = "Same as source";

    public static LPresetRecord LPresetRecordCreate(LPreset lState) => new()
    {
        LPresetName = lState.LPresetName,
        LPresetDisplay = lState.LPresetDisplay,
        LPresetContainer = lState.LPresetContainer,
        LPresetExtension = lState.LPresetExtension,
        LPresetExportMode = lState.LPresetExportMode,
        LPresetVideoStream = lState.LPresetVideoStream,
        LPresetAudioStream = lState.LPresetAudioStream,
        LPresetVideoMode = lState.LPresetVideoMode,
        LPresetAudioMode = lState.LPresetAudioMode,
        LPresetVideoEncoder = lState.LPresetVideoEncoder,
        LPresetRateControl = lState.LPresetRateControl,
        LPresetVideoQuality = lState.LPresetVideoQuality,
        LPresetSpeedPreset = lState.LPresetSpeedPreset,
        LPresetLocation = lState.LPresetLocation,
        LPresetLocationFolder = lState.LPresetLocationFolder,
        LPresetVideoSize = lState.LPresetVideoSize,
        LPresetSizeReactive = lState.LPresetSizeReactive,
        LPresetVideoFps = lState.LPresetVideoFps,
        LPresetPixelLayout = lState.LPresetPixelLayout,
        LPresetVideoExtras = new Dictionary<string, string>(lState.LPresetVideoExtras),
        LPresetAudioEncoder = lState.LPresetAudioEncoder,
        LPresetAudioBitrate = lState.LPresetAudioBitrate,
        LPresetSampleRate = lState.LPresetSampleRate,
        LPresetAudioChannels = lState.LPresetAudioChannels
    };

    public LPreset LPresetStateCreate() => new()
    {
        LPresetName = LPresetName,
        LPresetDisplay = LPresetDisplay,
        LPresetContainer = LPresetContainer,
        LPresetExtension = string.IsNullOrEmpty(LPresetExtension)
            ? LPreset.LPresetExtensionsRead(LPresetContainer).FirstOrDefault() ?? string.Empty
            : LPresetExtension,
        LPresetExportMode = LPresetExportMode,
        LPresetVideoStream = LPresetVideoStream,
        LPresetAudioStream = LPresetAudioStream,
        LPresetVideoMode = LPresetVideoMode,
        LPresetAudioMode = LPresetAudioMode,
        LPresetVideoEncoder = LPresetVideoEncoder,
        LPresetRateControl = LPresetRateControl,
        LPresetVideoQuality = LPresetVideoQuality,
        LPresetSpeedPreset = LPresetSpeedPreset,
        LPresetLocation = string.Equals(LPresetLocation, "Custom folder", StringComparison.Ordinal) ? "Custom location" : LPresetLocation,
        LPresetLocationFolder = LPresetLocationFolder,
        LPresetVideoSize = LPresetVideoSize,
        LPresetSizeReactive = LPresetSizeReactive,
        LPresetVideoFps = LPresetVideoFps,
        LPresetPixelLayout = LPresetPixelLayout,
        LPresetVideoExtras = new Dictionary<string, string>(LPresetVideoExtras, StringComparer.Ordinal),
        LPresetAudioEncoder = LPresetAudioEncoder,
        LPresetAudioBitrate = LPresetAudioBitrate,
        LPresetSampleRate = LPresetSampleRate,
        LPresetAudioChannels = LPresetAudioChannels
    };
}
