using Cadroue.Core;

namespace Cadroue.ShellEngine;

public interface LRosterDetailPort
{
    // The schedule's work item with this id as the roster's detail reads it, or null when the schedule holds none.
    // Ownership is told against the station's runner. The place names the tab the detail is headed with; it is asked
    // once the item is found, never before.
    LRosterJob? LRosterDetailRead(
        LStation lStation,
        Guid lWorkId,
        Func<(Guid LRosterPlaceBatch, Guid LRosterPlaceRelay, string LRosterPlaceTab)> lPlace);
}

// One work item as the roster's detail reads it. A failure or a message is null when it is not shown; the tab is null
// when the title is blank; the spent time is null before the start or when it would run backwards.
// The owner token is null while nobody claims the item; the state token names its state, or its phase while it runs.
public sealed record LRosterJob(
    string? LRosterJobFailure,
    string? LRosterJobMessage,
    string? LRosterJobTab,
    TimeSpan? LRosterJobSpent,
    bool LRosterJobMeasured,
    long? LRosterJobBytes,
    LRosterSide LRosterJobSource,
    LRosterSide LRosterJobOutput,
    bool LRosterJobSound,
    IReadOnlyList<string> LRosterJobSources,
    DateTimeOffset LRosterJobCreated,
    DateTimeOffset? LRosterJobStarted,
    DateTimeOffset? LRosterJobFinished,
    int LRosterJobAttempts,
    string? LRosterJobOwner,
    string LRosterJobState,
    bool LRosterJobHigh,
    LRosterEncoding LRosterJobVideo,
    LRosterEncoding LRosterJobAudio)
{
    public static LRosterJob LRosterJobCreate(
        LWorkItem lWorkItem, bool lOwned, string lTitle, Func<DateTimeOffset> lClock)
    {
        bool lFailed = lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateFailed;
        bool lMessage = !string.IsNullOrWhiteSpace(lWorkItem.LWorkMessage);
        LWorkMedia? lSourceInfo = lWorkItem.LWorkSourceMedia;
        LWorkMedia? lOutputInfo = lWorkItem.LWorkOutputMedia;
        bool lSourceAudio = (lSourceInfo?.LWorkMediaSamplerate ?? 0) > 0;
        bool lOutputAudio = (lOutputInfo?.LWorkMediaSamplerate ?? 0) > 0;
        double? lSourceLoudness = lSourceAudio ? lSourceInfo?.LWorkMediaLoudness : null;
        double? lOutputLoudness = lWorkItem.LWorkAudio.LWorkAudioActive
            ? (lOutputAudio ? lOutputInfo?.LWorkMediaLoudness : null)
            : lSourceLoudness;
        return new LRosterJob(
            lFailed && lMessage ? lWorkItem.LWorkMessage : null,
            !lFailed && lMessage ? lWorkItem.LWorkMessage : null,
            string.IsNullOrWhiteSpace(lTitle) ? null : lTitle,
            LRosterSpentRead(lWorkItem, lClock),
            lWorkItem.LWorkSourceMeasured,
            lWorkItem.LWorkOutputBytes is { } lBytes && lBytes >= 0 ? lBytes : null,
            LRosterSide.LRosterSideCreate(
                lSourceInfo, lWorkItem.LWorkSourcePath, lSourceLoudness, lSourceInfo?.LWorkMediaDuration),
            LRosterSide.LRosterSideCreate(
                lOutputInfo,
                lWorkItem.LWorkOutputPath,
                lOutputLoudness,
                lOutputInfo?.LWorkMediaDuration ?? lWorkItem.LWorkDuration),
            lSourceAudio || lOutputAudio,
            lWorkItem.LWorkMergeSources.Count > 1 ? lWorkItem.LWorkMergeSources : [lWorkItem.LWorkSourcePath],
            lWorkItem.LWorkCreateTime,
            lWorkItem.LWorkStartTime,
            lWorkItem.LWorkFinishTime,
            lWorkItem.LWorkAttemptCount,
            lWorkItem.LWorkOwnerRunner == Guid.Empty
                ? null
                : lOwned
                    ? "ThisTab"
                    : lWorkItem.LWorkOwnerProcess == Environment.ProcessId ? "OtherTab" : "OtherWindow",
            lWorkItem.LWorkStateCurrent switch
            {
                LWorkState.LWorkStateDone => "Done",
                LWorkState.LWorkStateFailed => "Failed",
                LWorkState.LWorkStateUnresolved => "Unresolved",
                LWorkState.LWorkStatePartial => "Partial",
                LWorkState.LWorkStateBlocked => "Blocked",
                _ => lWorkItem.LWorkPhaseCurrent switch
                {
                    LWorkPhase.LWorkPhaseEncoding => "Processing",
                    LWorkPhase.LWorkPhaseStarted => "Started",
                    _ => "NotStarted"
                }
            },
            lWorkItem.LWorkPriority == LWorkPriority.LWorkPriorityHigh,
            LRosterEncoding.LRosterEncodingCreate(lWorkItem.LWorkOutput.LEncodingVideo),
            LRosterEncoding.LRosterEncodingCreate(lWorkItem.LWorkOutput.LEncodingAudio));
    }

    // The time an item has spent since its start, up to its finish; the clock is read only while it runs.
    private static TimeSpan? LRosterSpentRead(LWorkItem lWorkItem, Func<DateTimeOffset> lClock)
    {
        if (lWorkItem.LWorkStartTime is not { } lStarted)
        {
            return null;
        }

        TimeSpan lSpent = (lWorkItem.LWorkFinishTime ?? lClock()) - lStarted;
        return lSpent < TimeSpan.Zero ? null : lSpent;
    }
}

// One side of the comparison, the source or the output. The media flag tells whether the side was measured; the
// keyframe spacing is in seconds and null when unknown. The codec, kilobits and sample rate are null without sound,
// the codec also when blank and the kilobits when no bitrate is known.
public sealed record LRosterSide(
    bool LRosterSideMedia,
    bool LRosterSideVideo,
    int LRosterSideWidth,
    int LRosterSideHeight,
    double LRosterSideFramerate,
    double? LRosterSideKeyframe,
    TimeSpan? LRosterSideDuration,
    string? LRosterSideCodec,
    double? LRosterSideKilobits,
    int? LRosterSideSamplerate,
    double? LRosterSideLoudness,
    string LRosterSidePath)
{
    public static LRosterSide LRosterSideCreate(
        LWorkMedia? lMediaInfo, string lPath, double? lLoudness, TimeSpan? lDuration)
    {
        bool lAudio = lMediaInfo is { LWorkMediaSamplerate: > 0 };
        return new LRosterSide(
            lMediaInfo is not null,
            lMediaInfo is { LWorkMediaVideo: true },
            lMediaInfo?.LWorkMediaWidth ?? 0,
            lMediaInfo?.LWorkMediaHeight ?? 0,
            lMediaInfo?.LWorkMediaFramerate ?? 0,
            lMediaInfo?.LWorkKeyframeInterval is { } lInterval && lInterval > 0 ? lInterval / 1000d : null,
            lDuration,
            lAudio && !string.IsNullOrWhiteSpace(lMediaInfo!.LWorkMediaCodec) ? lMediaInfo.LWorkMediaCodec : null,
            lMediaInfo is { LWorkMediaSamplerate: > 0, LWorkMediaBitrate: > 0 }
                ? Math.Round(lMediaInfo.LWorkMediaBitrate / 1000d)
                : null,
            lAudio ? lMediaInfo!.LWorkMediaSamplerate : null,
            lLoudness,
            lPath);
    }
}

// One stream's encoding plan. Reencode is false when the stream is copied or excluded. The speed is the video's preset,
// or the audio's speed and null when blank; extras are null when there are none. A field the stream lacks reads empty.
public sealed record LRosterEncoding(
    string LRosterEncodingMode,
    string LRosterEncodingStream,
    bool LRosterEncodingReencode,
    string LRosterEncodingEncoder,
    string LRosterEncodingControl,
    string LRosterEncodingQuality,
    string? LRosterEncodingSpeed,
    string LRosterEncodingPixel,
    string LRosterEncodingSize,
    string LRosterEncodingFramerate,
    IReadOnlyDictionary<string, string>? LRosterEncodingExtras,
    string LRosterEncodingSamplerate,
    string LRosterEncodingChannels)
{
    public static LRosterEncoding LRosterEncodingCreate(LEncodingVideo lVideo) =>
        new(
            lVideo.LEncodingMode,
            lVideo.LEncodingStream,
            LRosterEncodingCheck(lVideo.LEncodingMode),
            lVideo.LEncodingEncoder,
            lVideo.LEncodingRateControl,
            lVideo.LEncodingQuality,
            lVideo.LEncodingSpeedPreset,
            lVideo.LEncodingPixel,
            lVideo.LEncodingSize,
            lVideo.LEncodingFps,
            lVideo.LEncodingExtras.Count > 0 ? lVideo.LEncodingExtras : null,
            string.Empty,
            string.Empty);

    public static LRosterEncoding LRosterEncodingCreate(LEncodingAudio lAudio) =>
        new(
            lAudio.LEncodingMode,
            lAudio.LEncodingStream,
            LRosterEncodingCheck(lAudio.LEncodingMode),
            lAudio.LEncodingEncoder,
            lAudio.LEncodingRateControl,
            lAudio.LEncodingQuality,
            string.IsNullOrWhiteSpace(lAudio.LEncodingSpeed) ? null : lAudio.LEncodingSpeed,
            string.Empty,
            string.Empty,
            string.Empty,
            lAudio.LEncodingExtras.Count > 0 ? lAudio.LEncodingExtras : null,
            lAudio.LEncodingSampleRate,
            lAudio.LEncodingChannels);

    // A mode reencodes unless it copies or excludes the stream, in any letter case.
    public static bool LRosterEncodingCheck(string lMode) =>
        !string.Equals(lMode, "Copy", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(lMode, "Exclude", StringComparison.OrdinalIgnoreCase);
}
