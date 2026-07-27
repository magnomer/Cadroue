using System.Text.Json;

namespace Cadroue.Core;

public sealed class LWorkRecord
{
    public Guid WorkId { get; set; }
    public Guid BatchId { get; set; }
    public string Kind { get; set; } = nameof(LWorkKind.LWorkKindSplit);
    public string Priority { get; set; } = nameof(LWorkPriority.LWorkPriorityNormal);
    public string State { get; set; } = nameof(LWorkState.LWorkStatePending);
    public string SourcePath { get; set; } = string.Empty;
    public long StartTicks { get; set; }
    public long EndTicks { get; set; }
    public string OutputName { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public double Progress { get; set; }
    public DateTimeOffset CreateTime { get; set; } = DateTimeOffset.Now;

    public int OwnerProcessId { get; set; }

    public LWorkOutputRecord Output { get; set; } = new();

    public static LWorkRecord LWorkRecordCreate(LWorkItem lWorkItem) => new()
    {
        WorkId = lWorkItem.LWorkId,
        BatchId = lWorkItem.LWorkBatchId,
        Kind = lWorkItem.LWorkKind.ToString(),
        Priority = lWorkItem.LWorkPriority.ToString(),
        State = lWorkItem.LWorkStateCurrent.ToString(),
        SourcePath = lWorkItem.LWorkSourcePath,
        StartTicks = lWorkItem.LWorkStart.Ticks,
        EndTicks = lWorkItem.LWorkEnd.Ticks,
        OutputName = lWorkItem.LWorkOutputName,
        OutputPath = lWorkItem.LWorkOutputPath,
        Message = lWorkItem.LWorkMessage,
        Progress = lWorkItem.LWorkProgress,
        CreateTime = lWorkItem.LWorkCreateTime,
        Output = LWorkOutputRecord.LWorkOutputRecordCreate(lWorkItem.LWorkOutput)
    };

    public LWorkItem LWorkItemCreate()
    {
        var lWorkItem = new LWorkItem(
            BatchId,
            LWorkEnumRead(Kind, LWorkKind.LWorkKindSplit),
            LWorkEnumRead(Priority, LWorkPriority.LWorkPriorityNormal),
            SourcePath,
            TimeSpan.FromTicks(StartTicks),
            TimeSpan.FromTicks(EndTicks),
            OutputName,
            OutputPath,
            Output.LWorkOutputCreate(),
            WorkId,
            CreateTime);

        lWorkItem.LWorkStateCurrent = LWorkEnumRead(State, LWorkState.LWorkStatePending);
        lWorkItem.LWorkMessage = Message;
        lWorkItem.LWorkProgress = Progress;
        return lWorkItem;
    }

    public string LWorkRecordJsonCreate() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static LWorkRecord? LWorkRecordParse(string lWorkJson)
    {
        try
        {
            return JsonSerializer.Deserialize<LWorkRecord>(lWorkJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TEnum LWorkEnumRead<TEnum>(string lWorkValue, TEnum lWorkFallback) where TEnum : struct =>
        Enum.TryParse(lWorkValue, out TEnum lWorkParsed) ? lWorkParsed : lWorkFallback;
}

public sealed class LWorkOutputRecord
{
    public string NamePattern { get; set; } = string.Empty;
    public string Container { get; set; } = "MP4";
    public string Extension { get; set; } = ".mp4";
    public string Location { get; set; } = "Same as source";
    public string LocationFolder { get; set; } = string.Empty;
    public string ExportMode { get; set; } = "Smart export";
    public string VideoStream { get; set; } = "Include";
    public string VideoMode { get; set; } = "Auto";
    public string VideoEncoder { get; set; } = string.Empty;
    public string RateControl { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public string SpeedPreset { get; set; } = string.Empty;
    public string VideoSize { get; set; } = "Same as source";
    public string VideoFps { get; set; } = "Same as source";
    public string PixelFormat { get; set; } = "Auto";
    public Dictionary<string, string> VideoExtras { get; set; } = new();
    public string AudioStream { get; set; } = string.Empty;
    public string AudioMode { get; set; } = "Auto";
    public string AudioEncoder { get; set; } = string.Empty;
    public string AudioBitrate { get; set; } = string.Empty;
    public string AudioSampleRate { get; set; } = "Same as source";
    public string AudioChannels { get; set; } = "Same as source";

    public static LWorkOutputRecord LWorkOutputRecordCreate(LWorkOutput lWorkOutput) => new()
    {
        NamePattern = lWorkOutput.LWorkOutputNamePattern,
        Container = lWorkOutput.LWorkOutputContainer,
        Extension = lWorkOutput.LWorkOutputExtension,
        Location = lWorkOutput.LWorkOutputLocation,
        LocationFolder = lWorkOutput.LWorkOutputLocationFolder,
        ExportMode = lWorkOutput.LWorkOutputExportMode,
        VideoStream = lWorkOutput.LWorkOutputVideoStream,
        VideoMode = lWorkOutput.LWorkOutputVideoMode,
        VideoEncoder = lWorkOutput.LWorkOutputVideoEncoder,
        RateControl = lWorkOutput.LWorkOutputRateControl,
        Quality = lWorkOutput.LWorkOutputQuality,
        SpeedPreset = lWorkOutput.LWorkOutputSpeedPreset,
        VideoSize = lWorkOutput.LWorkOutputVideoSize,
        VideoFps = lWorkOutput.LWorkOutputVideoFps,
        PixelFormat = lWorkOutput.LWorkOutputPixelFormat,
        VideoExtras = new Dictionary<string, string>(lWorkOutput.LWorkOutputVideoExtras, StringComparer.Ordinal),
        AudioStream = lWorkOutput.LWorkOutputAudioStream,
        AudioMode = lWorkOutput.LWorkOutputAudioMode,
        AudioEncoder = lWorkOutput.LWorkOutputAudioEncoder,
        AudioBitrate = lWorkOutput.LWorkOutputAudioBitrate,
        AudioSampleRate = lWorkOutput.LWorkOutputAudioSampleRate,
        AudioChannels = lWorkOutput.LWorkOutputAudioChannels
    };

    public LWorkOutput LWorkOutputCreate() => new(
        NamePattern,
        Container,
        Extension,
        Location,
        LocationFolder,
        ExportMode,
        VideoStream,
        VideoMode,
        VideoEncoder,
        RateControl,
        Quality,
        SpeedPreset,
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
}
