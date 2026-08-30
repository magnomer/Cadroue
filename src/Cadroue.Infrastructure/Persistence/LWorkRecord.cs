using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

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
    public Guid LWorkRelaySource { get; set; }
    public Guid LWorkLineage { get; set; }
    public string LWorkTab { get; set; } = string.Empty;
    public string LWorkMessage { get; set; } = string.Empty;
    public double LWorkProgress { get; set; }
    public DateTimeOffset LWorkCreateTime { get; set; } = DateTimeOffset.Now;

    public DateTimeOffset? LWorkStartTime { get; set; }

    public DateTimeOffset? LWorkFinishTime { get; set; }

    public long? LWorkOutputBytes { get; set; }

    public long? LWorkSourceBytes { get; set; }

    public List<long> LWorkMergeBytes { get; set; } = [];

    public LWorkMedia? LWorkSourceMedia { get; set; }

    public LWorkMedia? LWorkOutputMedia { get; set; }

    public int LWorkOwnerProcess { get; set; }

    public long LWorkOwnerStamp { get; set; }

    public Guid LWorkOwnerRunner { get; set; }

    public Guid LWorkSignet { get; set; }

    public DateTimeOffset LWorkLeaseTime { get; set; }

    public string LWorkPhaseName { get; set; } = nameof(LWorkPhase.LWorkPhaseNone);

    public int LWorkAttemptCount { get; set; }

    public int LWorkRecoverCount { get; set; }

    public LWorkOutputRecord LWorkOutputSnapshot { get; set; } = new();

    public LWorkCrop LWorkCrop { get; set; } = LWorkCrop.LWorkCropCreate();

    public LWorkVideo LWorkVideo { get; set; } = LWorkVideo.LWorkVideoCreate();

    public LWorkAudio LWorkAudio { get; set; } = LWorkAudio.LWorkAudioCreate();

    public List<LDossier> LWorkDossiers { get; set; } = [];

    public LWorkFix LWorkFixPlan { get; set; } = LWorkFix.LWorkFixCreate();

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
        LWorkRelaySource = lWorkItem.LWorkRelaySource,
        LWorkLineage = lWorkItem.LWorkLineage,
        LWorkTab = lWorkItem.LWorkTab,
        LWorkMessage = lWorkItem.LWorkMessage,
        LWorkProgress = lWorkItem.LWorkProgress,
        LWorkCreateTime = lWorkItem.LWorkCreateTime,
        LWorkStartTime = lWorkItem.LWorkStartTime,
        LWorkFinishTime = lWorkItem.LWorkFinishTime,
        LWorkOutputBytes = lWorkItem.LWorkOutputBytes,
        LWorkSourceBytes = lWorkItem.LWorkSourceBytes,
        LWorkMergeBytes = lWorkItem.LWorkMergeBytes.ToList(),
        LWorkSourceMedia = lWorkItem.LWorkSourceMedia,
        LWorkOutputMedia = lWorkItem.LWorkOutputMedia,
        LWorkOwnerProcess = lWorkItem.LWorkOwnerProcess,
        LWorkOwnerStamp = lWorkItem.LWorkOwnerStamp,
        LWorkOwnerRunner = lWorkItem.LWorkOwnerRunner,
        LWorkSignet = lWorkItem.LWorkSignet,
        LWorkPhaseName = lWorkItem.LWorkPhaseCurrent.ToString(),
        LWorkAttemptCount = lWorkItem.LWorkAttemptCount,
        LWorkRecoverCount = lWorkItem.LWorkRecoverCount,
        LWorkOutputSnapshot = LWorkOutputRecord.LWorkSnapshotCreate(lWorkItem.LWorkOutput),
        LWorkCrop = lWorkItem.LWorkCrop,
        LWorkVideo = lWorkItem.LWorkVideo,
        LWorkAudio = lWorkItem.LWorkAudio,
        LWorkDossiers = lWorkItem.LWorkDossiers.ToList(),
        LWorkFixPlan = lWorkItem.LWorkFixPlan
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
        lWorkItem.LWorkRelaySource = LWorkRelaySource;
        lWorkItem.LWorkLineage = LWorkLineage;
        lWorkItem.LWorkTab = LWorkTab;
        lWorkItem.LWorkStateCurrent = LWorkEnumRead(LWorkStateName, LWorkState.LWorkStatePending);
        lWorkItem.LWorkMessage = LWorkMessage;
        lWorkItem.LWorkProgress = LWorkProgress;
        lWorkItem.LWorkOwnerProcess = LWorkOwnerProcess;
        lWorkItem.LWorkOwnerStamp = LWorkOwnerStamp;
        lWorkItem.LWorkOwnerRunner = LWorkOwnerRunner;
        lWorkItem.LWorkSignet = LWorkSignet;
        lWorkItem.LWorkPhaseCurrent = LWorkEnumRead(LWorkPhaseName, LWorkPhase.LWorkPhaseNone);
        lWorkItem.LWorkAttemptCount = LWorkAttemptCount;
        lWorkItem.LWorkRecoverCount = LWorkRecoverCount;
        lWorkItem.LWorkStartTime = LWorkStartTime;
        lWorkItem.LWorkFinishTime = LWorkFinishTime;
        lWorkItem.LWorkOutputBytes = LWorkOutputBytes;
        lWorkItem.LWorkSourceBytes = LWorkSourceBytes;
        lWorkItem.LWorkMergeBytes = LWorkMergeBytes.ToArray();
        lWorkItem.LWorkSourceMedia = LWorkSourceMedia;
        lWorkItem.LWorkSourceMeasured = true;
        lWorkItem.LWorkOutputMedia = LWorkOutputMedia;
        lWorkItem.LWorkDossiers = LWorkDossiers.ToArray();
        lWorkItem.LWorkFixPlan = LWorkFixPlan;
        return lWorkItem;
    }

    public string LWorkJsonCreate() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static LWorkRecord? LWorkRecordParse(string lWorkJson)
    {
        try
        {
            LWorkRecord? lWorkRecord = JsonSerializer.Deserialize<LWorkRecord>(lWorkJson);
            lWorkRecord?.LWorkRecordNormalize();
            return lWorkRecord;
        }
        catch (Exception lWorkException) when (lWorkException is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private void LWorkRecordNormalize()
    {
        LWorkKindName ??= nameof(LWorkKind.LWorkKindSplit);
        LWorkPriorityName ??= nameof(LWorkPriority.LWorkPriorityNormal);
        LWorkStateName ??= nameof(LWorkState.LWorkStatePending);
        LWorkSourcePath ??= string.Empty;
        LWorkTab ??= string.Empty;
        LWorkOutputName ??= string.Empty;
        LWorkOutputPath ??= string.Empty;
        LWorkMessage ??= string.Empty;
        LWorkPhaseName ??= nameof(LWorkPhase.LWorkPhaseNone);
        LWorkMergeSources ??= [];
        LWorkMergeBytes ??= [];
        LWorkCrop ??= LWorkCrop.LWorkCropCreate();
        LWorkVideo ??= LWorkVideo.LWorkVideoCreate();
        LWorkAudio ??= LWorkAudio.LWorkAudioCreate();
        LWorkDossiers ??= [];
        LWorkFixPlan = LWorkFixPlan is { LWorkFixSteps: not null } lWorkFixPlan
            ? lWorkFixPlan
            : LWorkFix.LWorkFixCreate();
        (LWorkOutputSnapshot ??= new()).LWorkOutputNormalize();
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
    public LWorkVideoRecord LWorkOutputVideo { get; set; } = new();
    public LWorkAudioRecord LWorkOutputAudio { get; set; } = new();
    public string LWorkPresetName { get; set; } = string.Empty;
    public string LWorkCollision { get; set; } = "Overwrite";
    public string LWorkCollisionSuffix { get; set; } = string.Empty;

    public static LWorkOutputRecord LWorkSnapshotCreate(LEncoding lEncoding) => new()
    {
        LWorkNamePattern = lEncoding.LEncodingNamePattern,
        LWorkContainer = lEncoding.LEncodingContainer,
        LWorkExtension = lEncoding.LEncodingExtension,
        LWorkLocation = lEncoding.LEncodingLocation,
        LWorkLocationFolder = lEncoding.LEncodingLocationFolder,
        LWorkOutputVideo = new LWorkVideoRecord
        {
            LWorkStream = lEncoding.LEncodingVideo.LEncodingStream,
            LWorkMode = lEncoding.LEncodingVideo.LEncodingMode,
            LWorkEncoder = lEncoding.LEncodingVideo.LEncodingEncoder,
            LWorkRateControl = lEncoding.LEncodingVideo.LEncodingRateControl,
            LWorkQuality = lEncoding.LEncodingVideo.LEncodingQuality,
            LWorkSpeedPreset = lEncoding.LEncodingVideo.LEncodingSpeedPreset,
            LWorkSize = lEncoding.LEncodingVideo.LEncodingSize,
            LWorkSizeReactive = lEncoding.LEncodingVideo.LEncodingSizeReactive,
            LWorkFps = lEncoding.LEncodingVideo.LEncodingFps,
            LWorkPixelLayout = lEncoding.LEncodingVideo.LEncodingPixel,
            LWorkExtras = new Dictionary<string, string>(lEncoding.LEncodingVideo.LEncodingExtras, StringComparer.Ordinal)
        },
        LWorkOutputAudio = new LWorkAudioRecord
        {
            LWorkStream = lEncoding.LEncodingAudio.LEncodingStream,
            LWorkMode = lEncoding.LEncodingAudio.LEncodingMode,
            LWorkEncoder = lEncoding.LEncodingAudio.LEncodingEncoder,
            LWorkRateControl = lEncoding.LEncodingAudio.LEncodingRateControl,
            LWorkQuality = lEncoding.LEncodingAudio.LEncodingQuality,
            LWorkSpeed = lEncoding.LEncodingAudio.LEncodingSpeed,
            LWorkExtras = new Dictionary<string, string>(lEncoding.LEncodingAudio.LEncodingExtras, StringComparer.Ordinal),
            LWorkSampleRate = lEncoding.LEncodingAudio.LEncodingSampleRate,
            LWorkChannels = lEncoding.LEncodingAudio.LEncodingChannels
        },
        LWorkPresetName = lEncoding.LEncodingPresetName,
        LWorkCollision = lEncoding.LEncodingCollision,
        LWorkCollisionSuffix = lEncoding.LEncodingCollisionSuffix
    };

    public void LWorkOutputNormalize()
    {
        LWorkNamePattern ??= string.Empty;
        LWorkContainer ??= "MP4";
        LWorkExtension ??= ".mp4";
        LWorkLocation ??= "Same as source";
        LWorkLocationFolder ??= string.Empty;
        (LWorkOutputVideo ??= new()).LWorkVideoNormalize();
        (LWorkOutputAudio ??= new()).LWorkAudioNormalize();
        LWorkPresetName ??= string.Empty;
        LWorkCollision ??= "Overwrite";
        LWorkCollisionSuffix ??= string.Empty;
    }

    public LEncoding LWorkOutputCreate() => new(
        LWorkNamePattern,
        LWorkContainer,
        LWorkExtension,
        LWorkLocation,
        LWorkLocationFolder,
        new LEncodingVideo(
            LWorkOutputVideo.LWorkStream,
            LWorkOutputVideo.LWorkMode,
            LWorkOutputVideo.LWorkEncoder,
            LWorkOutputVideo.LWorkRateControl,
            LWorkOutputVideo.LWorkQuality,
            LWorkOutputVideo.LWorkSpeedPreset,
            LWorkOutputVideo.LWorkSize,
            LWorkOutputVideo.LWorkSizeReactive,
            LWorkOutputVideo.LWorkFps,
            LWorkOutputVideo.LWorkPixelLayout,
            new Dictionary<string, string>(LWorkOutputVideo.LWorkExtras, StringComparer.Ordinal)),
        new LEncodingAudio(
            LWorkOutputAudio.LWorkStream,
            LWorkOutputAudio.LWorkMode,
            LWorkOutputAudio.LWorkEncoder,
            LWorkOutputAudio.LWorkRateControl,
            LWorkOutputAudio.LWorkQuality,
            LWorkOutputAudio.LWorkSpeed,
            new Dictionary<string, string>(LWorkOutputAudio.LWorkExtras, StringComparer.Ordinal),
            LWorkOutputAudio.LWorkSampleRate,
            LWorkOutputAudio.LWorkChannels),
        LWorkPresetName,
        LWorkCollision,
        LWorkCollisionSuffix);
}

public sealed class LWorkVideoRecord
{
    public string LWorkStream { get; set; } = "Include";
    public string LWorkMode { get; set; } = "Auto";
    public string LWorkEncoder { get; set; } = string.Empty;
    public string LWorkRateControl { get; set; } = string.Empty;
    public string LWorkQuality { get; set; } = string.Empty;
    public string LWorkSpeedPreset { get; set; } = string.Empty;
    public string LWorkSize { get; set; } = "Same as source";
    public bool LWorkSizeReactive { get; set; }
    public string LWorkFps { get; set; } = "Same as source";
    public string LWorkPixelLayout { get; set; } = "Auto";
    public Dictionary<string, string> LWorkExtras { get; set; } = new();

    public void LWorkVideoNormalize()
    {
        LWorkStream ??= "Include";
        LWorkMode = LWorkMode switch
        {
            "Copy" or "Smart" or "Encode" => LWorkMode,
            _ => "Encode"
        };
        LWorkEncoder ??= string.Empty;
        LWorkRateControl ??= string.Empty;
        LWorkQuality ??= string.Empty;
        LWorkSpeedPreset ??= string.Empty;
        LWorkSize ??= "Same as source";
        LWorkFps ??= "Same as source";
        LWorkPixelLayout ??= "Auto";
        LWorkExtras ??= new();
    }
}

public sealed class LWorkAudioRecord
{
    public string LWorkStream { get; set; } = string.Empty;
    public string LWorkMode { get; set; } = "Auto";
    public string LWorkEncoder { get; set; } = string.Empty;
    public string LWorkRateControl { get; set; } = string.Empty;
    public string LWorkQuality { get; set; } = string.Empty;
    public string LWorkSpeed { get; set; } = string.Empty;
    public Dictionary<string, string> LWorkExtras { get; set; } = new();
    public string LWorkSampleRate { get; set; } = "Same as source";
    public string LWorkChannels { get; set; } = "Same as source";

    public void LWorkAudioNormalize()
    {
        LWorkStream ??= string.Empty;
        LWorkMode ??= "Auto";
        LWorkEncoder ??= string.Empty;
        LWorkRateControl ??= string.Empty;
        LWorkQuality ??= string.Empty;
        LWorkSpeed ??= string.Empty;
        LWorkExtras ??= new();
        LWorkSampleRate ??= "Same as source";
        LWorkChannels ??= "Same as source";
    }
}
