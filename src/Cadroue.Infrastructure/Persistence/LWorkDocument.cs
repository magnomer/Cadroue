using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LWorkDocument
{
    public const int LWorkSchemaCurrent = 1;

    public int LWorkSchema { get; set; } = LWorkSchemaCurrent;
    public Guid LWorkId { get; set; }
    public Guid LWorkBatchId { get; set; }
    public string? LWorkKindName { get; set; }
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
    public int LWorkRelayProcess { get; set; }
    public long LWorkRelayStamp { get; set; }
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

    public bool? LWorkSourceMeasured { get; set; }

    public LWorkMedia? LWorkOutputMedia { get; set; }

    public int LWorkOwnerProcess { get; set; }

    public long LWorkOwnerStamp { get; set; }

    public Guid LWorkOwnerRunner { get; set; }

    public Guid LWorkSignet { get; set; }

    public DateTimeOffset LWorkLeaseTime { get; set; }

    public string LWorkPhaseName { get; set; } = nameof(LWorkPhase.LWorkPhaseNone);

    public int LWorkAttemptCount { get; set; }

    public int LWorkRecoverCount { get; set; }

    public int LWorkRetryCount { get; set; }

    public LWorkOutputEntry LWorkOutputSnapshot { get; set; } = new();

    public LWorkCrop LWorkCrop { get; set; } = LWorkCrop.LWorkCropCreate();

    public LWorkVideo LWorkVideo { get; set; } = LWorkVideo.LWorkVideoCreate();

    public LWorkAudio LWorkAudio { get; set; } = LWorkAudio.LWorkAudioCreate();

    public List<LDossier> LWorkDossiers { get; set; } = [];

    public LWorkFix LWorkFixPlan { get; set; } = LWorkFix.LWorkFixCreate();

    public static LWorkDocument LWorkRecordCreate(LWorkItem lWorkItem) => new()
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
        LWorkRelayProcess = lWorkItem.LWorkRelayProcess,
        LWorkRelayStamp = lWorkItem.LWorkRelayStamp,
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
        LWorkSourceMeasured = lWorkItem.LWorkSourceMeasured,
        LWorkOutputMedia = lWorkItem.LWorkOutputMedia,
        LWorkOwnerProcess = lWorkItem.LWorkOwnerProcess,
        LWorkOwnerStamp = lWorkItem.LWorkOwnerStamp,
        LWorkOwnerRunner = lWorkItem.LWorkOwnerRunner,
        LWorkSignet = lWorkItem.LWorkSignet,
        LWorkPhaseName = lWorkItem.LWorkPhaseCurrent.ToString(),
        LWorkAttemptCount = lWorkItem.LWorkAttemptCount,
        LWorkRecoverCount = lWorkItem.LWorkRecoverCount,
        LWorkRetryCount = lWorkItem.LWorkRetryCount,
        LWorkOutputSnapshot = LWorkOutputEntry.LWorkSnapshotCreate(lWorkItem.LWorkOutput),
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
            Enum.Parse<LWorkKind>(LWorkKindName!),
            Enum.Parse<LWorkPriority>(LWorkPriorityName),
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
        lWorkItem.LWorkRelayProcess = LWorkRelayProcess;
        lWorkItem.LWorkRelayStamp = LWorkRelayStamp;
        lWorkItem.LWorkLineage = LWorkLineage;
        lWorkItem.LWorkTab = LWorkTab;
        lWorkItem.LWorkStateCurrent = Enum.Parse<LWorkState>(LWorkStateName);
        lWorkItem.LWorkMessage = LWorkMessage;
        lWorkItem.LWorkProgress = LWorkProgress;
        lWorkItem.LWorkOwnerProcess = LWorkOwnerProcess;
        lWorkItem.LWorkOwnerStamp = LWorkOwnerStamp;
        lWorkItem.LWorkOwnerRunner = LWorkOwnerRunner;
        lWorkItem.LWorkSignet = LWorkSignet;
        lWorkItem.LWorkPhaseCurrent = Enum.Parse<LWorkPhase>(LWorkPhaseName);
        lWorkItem.LWorkAttemptCount = LWorkAttemptCount;
        lWorkItem.LWorkRecoverCount = LWorkRecoverCount;
        lWorkItem.LWorkRetryCount = LWorkRetryCount;
        lWorkItem.LWorkStartTime = LWorkStartTime;
        lWorkItem.LWorkFinishTime = LWorkFinishTime;
        lWorkItem.LWorkOutputBytes = LWorkOutputBytes;
        lWorkItem.LWorkSourceBytes = LWorkSourceBytes;
        lWorkItem.LWorkMergeBytes = LWorkMergeBytes.ToArray();
        lWorkItem.LWorkSourceMedia = LWorkSourceMedia;
        lWorkItem.LWorkSourceMeasured = LWorkSourceMeasured ?? true;
        lWorkItem.LWorkOutputMedia = LWorkOutputMedia;
        lWorkItem.LWorkDossiers = LWorkDossiers.ToArray();
        lWorkItem.LWorkFixPlan = LWorkFixPlan;
        return lWorkItem;
    }

    public string LWorkJsonCreate() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });

    public static LWorkDocument? LWorkRecordParse(string lWorkJson)
    {
        try
        {
            LWorkDocument? lWorkRecord = JsonSerializer.Deserialize<LWorkDocument>(lWorkJson);
            if (lWorkRecord is null)
            {
                return null;
            }

            lWorkRecord.LWorkRecordNormalize();
            return lWorkRecord.LWorkRecordCheck() ? lWorkRecord : null;
        }
        catch (Exception lWorkException) when (lWorkException is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private void LWorkRecordNormalize()
    {
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

    private bool LWorkRecordCheck() =>
        LWorkSchema <= LWorkSchemaCurrent
        && LWorkId != Guid.Empty
        && LWorkEnumCheck<LWorkKind>(LWorkKindName)
        && LWorkEnumCheck<LWorkPriority>(LWorkPriorityName)
        && LWorkEnumCheck<LWorkState>(LWorkStateName)
        && LWorkEnumCheck<LWorkPhase>(LWorkPhaseName)
        && LWorkStartTicks >= 0
        && (LWorkEndTicks <= 0 || LWorkEndTicks >= LWorkStartTicks)
        && !double.IsNaN(LWorkProgress);

    private static bool LWorkEnumCheck<LWorkEnum>(string? lWorkValue)
        where LWorkEnum : struct, Enum =>
        lWorkValue is not null
        && Enum.TryParse(lWorkValue, out LWorkEnum lWorkParsed)
        && Enum.IsDefined(lWorkParsed);
}
