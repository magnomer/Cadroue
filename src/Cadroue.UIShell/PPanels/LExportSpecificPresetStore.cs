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

    private static string LPresetPathCreate()
    {
        string lAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(lAppData, LPresetFolderName, LPresetFileName);
    }
}

internal sealed class LExportSpecificPresetRecord
{
    public string PresetName { get; set; } = "MP4_H264_AAC_Default";
    public string Name { get; set; } = "OriginalName_export";
    public string Container { get; set; } = "MP4";
    public string ExportMode { get; set; } = "Smart export";
    public string VideoStream { get; set; } = "Include";
    public string AudioStream { get; set; } = "Include first audio track";
    public string VideoMode { get; set; } = "Auto";
    public string AudioMode { get; set; } = "Auto";

    internal static LExportSpecificPresetRecord LPresetRecordCreate(LExportSpecificState lState) => new()
    {
        PresetName = lState.PresetName,
        Name = lState.Name,
        Container = lState.Container,
        ExportMode = lState.ExportMode,
        VideoStream = lState.VideoStream,
        AudioStream = lState.AudioStream,
        VideoMode = lState.VideoMode,
        AudioMode = lState.AudioMode
    };

    internal LExportSpecificState LPresetStateCreate() => new()
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
}
