using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The roster detail's gate: the selected item's facts and its lineage step, read in one engine call. CRoster holds one
// per roster and hands it the roster's station. It answers what the detail shows; the owner keeps wording and formats.
public sealed class CRosterDetail
{
    private readonly LRosterDetailPort _cRosterDetailPort;
    private LStation? _cRosterDetailStation;

    internal CRosterDetail(LRosterDetailPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cRosterDetailPort = port;
    }

    // The roster hands its station once; ownership is told against its runner.
    internal void CRosterDetailAttach(LStation station) => _cRosterDetailStation = station;

    // The item with this id, or null when the id is empty, the schedule no longer holds the item or the roster has no
    // station. The step is read only once the item is found.
    public CRosterJob? CRosterDetailRead(Guid work, Func<Guid, CLineageStep> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (_cRosterDetailStation is not { } station)
        {
            return null;
        }

        CLineageStep? held = null;
        LRosterJob? job = _cRosterDetailPort.LRosterDetailRead(station, work, () =>
        {
            held = step(work);
            return (held.CLineageStepBatch, held.CLineageStepRelay, held.CLineageStepTab);
        });
        return job is null ? null : CRosterJob.CRosterJobCreate(job, held ?? step(work));
    }
}

// The selected item as the detail shows it. A failure, a message or a tab is null when it is not shown, and the spent
// time is null unless it shows beside the size bar. The owner token is null while nobody claims the item; the state
// token names its state, or its phase while it runs. High tells the high priority.
public sealed record CRosterJob(
    string? CRosterJobFailure,
    string? CRosterJobMessage,
    string? CRosterJobTab,
    TimeSpan? CRosterJobSpent,
    bool CRosterJobMeasured,
    CRosterSide CRosterJobSource,
    CRosterSide CRosterJobOutput,
    bool CRosterJobSound,
    IReadOnlyList<string> CRosterJobSources,
    DateTimeOffset CRosterJobCreated,
    DateTimeOffset? CRosterJobStarted,
    DateTimeOffset? CRosterJobFinished,
    int CRosterJobAttempts,
    string? CRosterJobOwner,
    string CRosterJobState,
    bool CRosterJobHigh,
    CRosterEncoding CRosterJobVideo,
    CRosterEncoding CRosterJobAudio)
{
    // The step's size is the source's, unknown when negative; the size bar shows when the source has bytes and the
    // output's size is known.
    internal static CRosterJob CRosterJobCreate(LRosterJob job, CLineageStep step)
    {
        long? source = step.CLineageStepSource is { } bytes && bytes >= 0 ? bytes : null;
        bool bar = source > 0 && job.LRosterJobBytes is not null;
        return new CRosterJob(
            job.LRosterJobFailure,
            job.LRosterJobMessage,
            job.LRosterJobTab,
            bar ? job.LRosterJobSpent : null,
            job.LRosterJobMeasured,
            CRosterSide.CRosterSideCreate(job.LRosterJobSource, source, step.CLineageSourceExtension),
            CRosterSide.CRosterSideCreate(job.LRosterJobOutput, job.LRosterJobBytes, step.CLineageOutputExtension),
            job.LRosterJobSound,
            job.LRosterJobSources,
            job.LRosterJobCreated,
            job.LRosterJobStarted,
            job.LRosterJobFinished,
            job.LRosterJobAttempts,
            job.LRosterJobOwner,
            job.LRosterJobState,
            job.LRosterJobHigh,
            CRosterEncoding.CRosterEncodingCreate(job.LRosterJobVideo),
            CRosterEncoding.CRosterEncodingCreate(job.LRosterJobAudio));
    }
}

// One side of the comparison, the source or the output. Bytes are null when unknown; the media flag tells whether the
// side was measured; the keyframe spacing is in seconds and null when unknown. The codec, kilobits and sample rate are
// null without sound, the codec also when blank and the kilobits when no bitrate is known.
public sealed record CRosterSide(
    long? CRosterSideBytes,
    bool CRosterSideMedia,
    bool CRosterSideVideo,
    int CRosterSideWidth,
    int CRosterSideHeight,
    double CRosterSideFramerate,
    double? CRosterSideKeyframe,
    TimeSpan? CRosterSideDuration,
    string? CRosterSideCodec,
    double? CRosterSideKilobits,
    int? CRosterSideSamplerate,
    double? CRosterSideLoudness,
    string CRosterSidePath,
    string CRosterSideExtension)
{
    internal static CRosterSide CRosterSideCreate(LRosterSide side, long? bytes, string extension) =>
        new(
            bytes,
            side.LRosterSideMedia,
            side.LRosterSideVideo,
            side.LRosterSideWidth,
            side.LRosterSideHeight,
            side.LRosterSideFramerate,
            side.LRosterSideKeyframe,
            side.LRosterSideDuration,
            side.LRosterSideCodec,
            side.LRosterSideKilobits,
            side.LRosterSideSamplerate,
            side.LRosterSideLoudness,
            side.LRosterSidePath,
            extension);
}

// One stream's encoding plan. Reencode is false when the stream is copied or excluded. The speed is the video's preset,
// or the audio's speed and null when blank; extras are null when there are none. A field the stream lacks reads empty.
public sealed record CRosterEncoding(
    string CRosterEncodingMode,
    string CRosterEncodingStream,
    bool CRosterEncodingReencode,
    string CRosterEncodingEncoder,
    string CRosterEncodingControl,
    string CRosterEncodingQuality,
    string? CRosterEncodingSpeed,
    string CRosterEncodingPixel,
    string CRosterEncodingSize,
    string CRosterEncodingFramerate,
    IReadOnlyDictionary<string, string>? CRosterEncodingExtras,
    string CRosterEncodingSamplerate,
    string CRosterEncodingChannels)
{
    internal static CRosterEncoding CRosterEncodingCreate(LRosterEncoding encoding) =>
        new(
            encoding.LRosterEncodingMode,
            encoding.LRosterEncodingStream,
            encoding.LRosterEncodingReencode,
            encoding.LRosterEncodingEncoder,
            encoding.LRosterEncodingControl,
            encoding.LRosterEncodingQuality,
            encoding.LRosterEncodingSpeed,
            encoding.LRosterEncodingPixel,
            encoding.LRosterEncodingSize,
            encoding.LRosterEncodingFramerate,
            encoding.LRosterEncodingExtras,
            encoding.LRosterEncodingSamplerate,
            encoding.LRosterEncodingChannels);
}
