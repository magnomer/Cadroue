using System.Text.Json;

namespace Cadroue.Core;

public sealed class LWorkRecord
{
    public Guid LWorkId { get; set; }
    public Guid LWorkBatchId { get; set; }
    public string LWorkKindName { get; set; } = nameof(LWorkKind.LWorkKindSplit);
    public string LWorkPriorityName { get; set; } = nameof(LWorkPriority.LWorkPriorityNormal);
    public string LWorkStateName { get; set; } = nameof(LWorkState.LWorkStatePending);
    public string LWorkSourcePath { get; set; } = string.Empty;
    public long LWorkStartTicks { get; set; }
    public long LWorkEndTicks { get; set; }
    public string LWorkOutputName { get; set; } = string.Empty;
    public string LWorkOutputPath { get; set; } = string.Empty;
    public List<string> LWorkMergeSources { get; set; } = [];
    public Guid LWorkRelayTarget { get; set; }
    public Guid LWorkLineage { get; set; }
    public string LWorkMessage { get; set; } = string.Empty;
    public double LWorkProgress { get; set; }
    public DateTimeOffset LWorkCreateTime { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset? LWorkStartTime { get; set; }

    public DateTimeOffset? LWorkFinishTime { get; set; }

    public long? LWorkOutputBytes { get; set; }

    public long? LWorkSourceBytes { get; set; }

    public LWorkMedia? LWorkSourceMedia { get; set; }

    public LWorkMedia? LWorkOutputMedia { get; set; }

    public int LWorkOwnerProcess { get; set; }

    public Guid LWorkOwnerRunner { get; set; }

    public DateTimeOffset LWorkLeaseTime { get; set; }

    public string LWorkPhaseName { get; set; } = nameof(LWorkPhase.LWorkPhaseNone);

    public int LWorkAttemptCount { get; set; }

    public LWorkOutputRecord LWorkOutputSnapshot { get; set; } = new();

    public LWorkCrop LWorkCrop { get; set; } = LWorkCrop.LWorkCropCreate();

    public LWorkVideo LWorkVideo { get; set; } = LWorkVideo.LWorkVideoCreate();

    public LWorkAudio LWorkAudio { get; set; } = LWorkAudio.LWorkAudioCreate();

    public static LWorkRecord LWorkRecordCreate(LWorkItem lWorkItem) => new()
    {
        LWorkId = lWorkItem.LWorkId,
        LWorkBatchId = lWorkItem.LWorkBatchId,
        LWorkKindName = lWorkItem.LWorkKind.ToString(),
        LWorkPriorityName = lWorkItem.LWorkPriority.ToString(),
        LWorkStateName = lWorkItem.LWorkStateCurrent.ToString(),
        LWorkSourcePath = lWorkItem.LWorkSourcePath,
        LWorkStartTicks = lWorkItem.LWorkOrigin.Ticks,
        LWorkEndTicks = lWorkItem.LWorkEnd.Ticks,
        LWorkOutputName = lWorkItem.LWorkOutputName,
        LWorkOutputPath = lWorkItem.LWorkOutputPath,
        LWorkMergeSources = lWorkItem.LWorkMergeSources.ToList(),
        LWorkRelayTarget = lWorkItem.LWorkRelayTarget,
        LWorkLineage = lWorkItem.LWorkLineage,
        LWorkMessage = lWorkItem.LWorkMessage,
        LWorkProgress = lWorkItem.LWorkProgress,
        LWorkCreateTime = lWorkItem.LWorkCreateTime,
        LWorkStartTime = lWorkItem.LWorkStartTime,
        LWorkFinishTime = lWorkItem.LWorkFinishTime,
        LWorkOutputBytes = lWorkItem.LWorkOutputBytes,
        LWorkSourceBytes = lWorkItem.LWorkSourceBytes,
        LWorkSourceMedia = lWorkItem.LWorkSourceMedia,
        LWorkOutputMedia = lWorkItem.LWorkOutputMedia,
        LWorkOwnerProcess = lWorkItem.LWorkOwnerProcess,
        LWorkOwnerRunner = lWorkItem.LWorkOwnerRunner,
        LWorkPhaseName = lWorkItem.LWorkPhaseCurrent.ToString(),
        LWorkAttemptCount = lWorkItem.LWorkAttemptCount,
        LWorkOutputSnapshot = LWorkOutputRecord.LWorkSnapshotCreate(lWorkItem.LWorkOutput),
        LWorkCrop = lWorkItem.LWorkCrop,
        LWorkVideo = lWorkItem.LWorkVideo,
        LWorkAudio = lWorkItem.LWorkAudio
    };

    public LWorkItem LWorkItemCreate()
    {
        var lWorkItem = new LWorkItem(
            LWorkBatchId,
            LWorkEnumRead(LWorkKindName, LWorkKind.LWorkKindSplit),
            LWorkEnumRead(LWorkPriorityName, LWorkPriority.LWorkPriorityNormal),
            LWorkSourcePath,
            TimeSpan.FromTicks(LWorkStartTicks),
            TimeSpan.FromTicks(LWorkEndTicks),
            LWorkOutputName,
            LWorkOutputPath,
            LWorkOutputSnapshot.LWorkOutputCreate(),
            LWorkId,
            LWorkCreateTime,
            LWorkCrop,
            LWorkVideo,
            LWorkAudio,
            LWorkMergeSources);

        lWorkItem.LWorkRelayTarget = LWorkRelayTarget;
        lWorkItem.LWorkLineage = LWorkLineage;
        lWorkItem.LWorkStateCurrent = LWorkEnumRead(LWorkStateName, LWorkState.LWorkStatePending);
        lWorkItem.LWorkMessage = LWorkMessage;
        lWorkItem.LWorkProgress = LWorkProgress;
        lWorkItem.LWorkOwnerProcess = LWorkOwnerProcess;
        lWorkItem.LWorkOwnerRunner = LWorkOwnerRunner;
        lWorkItem.LWorkPhaseCurrent = LWorkEnumRead(LWorkPhaseName, LWorkPhase.LWorkPhaseNone);
        lWorkItem.LWorkAttemptCount = LWorkAttemptCount;
        lWorkItem.LWorkStartTime = LWorkStartTime;
        lWorkItem.LWorkFinishTime = LWorkFinishTime;
        lWorkItem.LWorkOutputBytes = LWorkOutputBytes;
        lWorkItem.LWorkSourceBytes = LWorkSourceBytes;
        lWorkItem.LWorkSourceMedia = LWorkSourceMedia;
        lWorkItem.LWorkOutputMedia = LWorkOutputMedia;
        return lWorkItem;
    }

    public string LWorkJsonCreate() =>
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
    public string LWorkNamePattern { get; set; } = string.Empty;
    public string LWorkContainer { get; set; } = "MP4";
    public string LWorkExtension { get; set; } = ".mp4";
    public string LWorkLocation { get; set; } = "Same as source";
    public string LWorkLocationFolder { get; set; } = string.Empty;
    public string LWorkExportMode { get; set; } = "Smart export";
    public string LWorkVideoStream { get; set; } = "Include";
    public string LWorkVideoMode { get; set; } = "Auto";
    public string LWorkVideoEncoder { get; set; } = string.Empty;
    public string LWorkRateControl { get; set; } = string.Empty;
    public string LWorkQuality { get; set; } = string.Empty;
    public string LWorkSpeedPreset { get; set; } = string.Empty;
    public string LWorkVideoSize { get; set; } = "Same as source";
    public bool LWorkSizeReactive { get; set; }
    public string LWorkVideoFps { get; set; } = "Same as source";
    public string LWorkPixelLayout { get; set; } = "Auto";
    public Dictionary<string, string> LWorkVideoExtras { get; set; } = new();
    public string LWorkAudioStream { get; set; } = string.Empty;
    public string LWorkAudioMode { get; set; } = "Auto";
    public string LWorkAudioEncoder { get; set; } = string.Empty;
    public string LWorkAudioBitrate { get; set; } = string.Empty;
    public string LWorkSampleRate { get; set; } = "Same as source";
    public string LWorkAudioChannels { get; set; } = "Same as source";
    public string LWorkPresetName { get; set; } = string.Empty;

    public static LWorkOutputRecord LWorkSnapshotCreate(LWorkOutput lWorkOutput) => new()
    {
        LWorkNamePattern = lWorkOutput.LWorkOutputNamePattern,
        LWorkContainer = lWorkOutput.LWorkOutputContainer,
        LWorkExtension = lWorkOutput.LWorkOutputExtension,
        LWorkLocation = lWorkOutput.LWorkOutputLocation,
        LWorkLocationFolder = lWorkOutput.LWorkOutputLocationFolder,
        LWorkExportMode = lWorkOutput.LWorkOutputExportMode,
        LWorkVideoStream = lWorkOutput.LWorkOutputVideoStream,
        LWorkVideoMode = lWorkOutput.LWorkOutputVideoMode,
        LWorkVideoEncoder = lWorkOutput.LWorkOutputVideoEncoder,
        LWorkRateControl = lWorkOutput.LWorkOutputRateControl,
        LWorkQuality = lWorkOutput.LWorkOutputQuality,
        LWorkSpeedPreset = lWorkOutput.LWorkOutputSpeedPreset,
        LWorkVideoSize = lWorkOutput.LWorkOutputVideoSize,
        LWorkSizeReactive = lWorkOutput.LWorkSizeReactive,
        LWorkVideoFps = lWorkOutput.LWorkOutputVideoFps,
        LWorkPixelLayout = lWorkOutput.LWorkOutputPixelFormat,
        LWorkVideoExtras = new Dictionary<string, string>(lWorkOutput.LWorkOutputVideoExtras, StringComparer.Ordinal),
        LWorkAudioStream = lWorkOutput.LWorkOutputAudioStream,
        LWorkAudioMode = lWorkOutput.LWorkOutputAudioMode,
        LWorkAudioEncoder = lWorkOutput.LWorkOutputAudioEncoder,
        LWorkAudioBitrate = lWorkOutput.LWorkOutputAudioBitrate,
        LWorkSampleRate = lWorkOutput.LWorkOutputAudioSampleRate,
        LWorkAudioChannels = lWorkOutput.LWorkOutputAudioChannels,
        LWorkPresetName = lWorkOutput.LWorkOutputPresetName
    };

    public LWorkOutput LWorkOutputCreate() => new(
        LWorkNamePattern,
        LWorkContainer,
        LWorkExtension,
        LWorkLocation,
        LWorkLocationFolder,
        LWorkExportMode,
        LWorkVideoStream,
        LWorkVideoMode,
        LWorkVideoEncoder,
        LWorkRateControl,
        LWorkQuality,
        LWorkSpeedPreset,
        LWorkVideoSize,
        LWorkSizeReactive,
        LWorkVideoFps,
        LWorkPixelLayout,
        new Dictionary<string, string>(LWorkVideoExtras, StringComparer.Ordinal),
        LWorkAudioStream,
        LWorkAudioMode,
        LWorkAudioEncoder,
        LWorkAudioBitrate,
        LWorkSampleRate,
        LWorkAudioChannels,
        LWorkPresetName);
}
