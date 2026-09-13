using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed partial class TEncodeCommand
{
    internal static readonly string TBridgeSource = Path.Combine("input media", "source clip.mov");
    internal static readonly string TBridgeOutput = Path.Combine("output media", "smart clip.mp4");

    internal static int TBridgeLabelFind(IReadOnlyList<LEncodeStage> stages, string label)
    {
        for (int index = 0; index < stages.Count; index++)
        {
            if (stages[index].LEncodeStageLabel == label)
            {
                return index;
            }
        }

        return -1;
    }

    internal static LWorkItem TBridgeWorkCreate(
        string source, string output, string codec = "h264", bool copyMode = true,
        string audioCodec = "aac", int sampleRate = 48000, string audioMode = "Copy",
        string audioStream = "Include")
    {
        LEncoding encoding = copyMode
            ? TOutputCreate(videoMode: "Smart", audioStream: audioStream, audioMode: audioMode)
            : TOutputCreate();
        LWorkItem work = TWorkCreate(
            LWorkKind.LWorkKindSplit, source, output, encoding,
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
        work.LWorkSourceMedia = new LWorkMedia(1920, 1080, 30, 30_000, true)
        {
            LWorkMediaCodec = codec,
            LWorkAudioCodec = audioCodec,
            LWorkMediaSamplerate = sampleRate
        };
        return work;
    }

    internal static LBridgeStream TSourceStreamCreate(
        string codec = "h264",
        string profile = "High",
        string pixel = "yuv420p",
        long bitrate = 5_000_000,
        string timeBase = "1/30000") =>
        new(codec, profile, pixel, "bt709", "bt709", "bt709", "tv", "30000/1001", bitrate, timeBase);

    internal static IReadOnlyList<LEncodeStage> TBridgeStagesBuild(
        LWorkItem work,
        LBridgeOutcome outcome,
        LBridgeSpan interval,
        LBridgeSpan? head,
        LBridgeSpan? middle,
        LBridgeSpan? tail,
        LBridgeStream? source = null,
        string? intermediateExtension = null) =>
        LEncode.LEncodeSmartBuild(
            work,
            new LBridgePlan(
                outcome,
                interval,
                head,
                middle,
                tail),
            source ?? TSourceStreamCreate(work.LWorkSourceMedia?.LWorkMediaCodec ?? "h264"),
            intermediateExtension);

    internal static IReadOnlyList<LEncodeStage> TBridgeResolveBuild(
        LWorkItem work,
        LBridgeOutcome outcome,
        LBridgeSpan interval,
        LBridgeSpan? head,
        LBridgeSpan? middle,
        LBridgeSpan? tail,
        LBridgeStream? source = null) =>
        LEncode.LEncodeSmartResolve(
            work,
            new LBridgePlan(
                outcome,
                interval,
                head,
                middle,
                tail),
            source ?? TSourceStreamCreate(work.LWorkSourceMedia?.LWorkMediaCodec ?? "h264"));

    internal static IReadOnlyList<LEncodeStage> TBridgePlanBuild(
        LWorkItem work,
        LBridgePlan plan,
        LBridgeStream? source = null,
        string? intermediateExtension = null) =>
        LEncode.LEncodeSmartBuild(
            work,
            plan,
            source ?? TSourceStreamCreate(work.LWorkSourceMedia?.LWorkMediaCodec ?? "h264"),
            intermediateExtension);

    internal static IReadOnlyList<LEncodeStage> TBridgeDecodeBuild(
        LWorkItem work,
        LBridgeSpan interval,
        LBridgeSpan? head,
        LBridgeSpan middle,
        LBridgeSpan? tail,
        LBridgeStream? source = null) =>
        TBridgePlanBuild(
            work,
            new LBridgePlan(
                LBridgeOutcome.LBridgeOutcomeSmart,
                interval,
                head,
                middle,
                tail),
            source);

    internal static IReadOnlyList<LEncodeStage> TBridgeResolve(
        LWorkItem work, params double[] keyframes) =>
        LEncode.LEncodeBridgeResolve(
            work, keyframes.Select(TimeSpan.FromSeconds).ToArray());

    internal static bool? TAudioIntervalRead(string source, double origin, double end) =>
        LScoutAudio.LScoutAudioRead(
            source, TimeSpan.FromSeconds(origin), TimeSpan.FromSeconds(end));

    internal static IReadOnlyList<LKeyframeEntry> TKeyframeRead(string source, double origin, double end) =>
        LKeyframeSeeker.LKeyframeRangeScan(
            source, TimeSpan.FromSeconds(origin), TimeSpan.FromSeconds(end));

    internal static string TToolFfmpegRead() => LTool.LToolFfmpegRead();

    internal static string TToolFfprobeRead() => LTool.LToolFfprobeRead();

    internal static LWorkItem TBridgeIntervalCreate(
        string source,
        string output,
        double origin,
        double end,
        string audioStream,
        string container = "matroska",
        string extension = "mkv")
    {
        LWorkItem work = TWorkCreate(
            LWorkKind.LWorkKindSplit,
            source,
            output,
            TOutputCreate(
                container: container,
                extension: extension,
                videoMode: "Smart",
                audioStream: audioStream,
                audioMode: "Copy"),
            TimeSpan.FromSeconds(origin),
            TimeSpan.FromSeconds(end));
        work.LWorkSourceMedia = new LWorkMedia(160, 90, 30, 8_000, true)
        {
            LWorkMediaCodec = "h264",
            LWorkAudioCodec = "aac",
            LWorkMediaSamplerate = 48_000
        };
        return work;
    }

    internal static LWorkItem TVideoIntervalCreate(
        string source,
        string output,
        double origin,
        double end,
        string videoMode)
    {
        LWorkItem work = TWorkCreate(
            LWorkKind.LWorkKindSplit,
            source,
            output,
            TOutputCreate(
                container: "matroska",
                extension: "mkv",
                videoMode: videoMode,
                audioStream: "Exclude",
                audioMode: "Exclude"),
            TimeSpan.FromSeconds(origin),
            TimeSpan.FromSeconds(end));
        work.LWorkSourceMedia = new LWorkMedia(
            160, 90, 30, Math.Max(12_000, (long)Math.Ceiling(end * 1_000)), false)
        {
            LWorkMediaCodec = "h264",
            LWorkAudioCodec = string.Empty
        };
        return work;
    }

    internal static IReadOnlyList<LEncodeStage> TBridgeSourceBuild(LWorkItem work)
    {
        IReadOnlyList<LKeyframeEntry> keyframes = LScoutBridge.LScoutBridgeRead(
            work.LWorkSourcePath, work.LWorkOrigin, work.LWorkEnd);
        LWorkMedia? media = LScout.LScoutMediaRead(work.LWorkSourcePath);
        bool openEnd = LBridge.LBridgeEndCheck(
            work.LWorkEnd,
            media?.LWorkMediaDuration ?? TimeSpan.Zero,
            media?.LWorkMediaFramerate ?? 0);
        LBridgePlan plan = LBridge.LBridgeRegionResolve(keyframes, work.LWorkOrigin, work.LWorkEnd, openEnd);
        return LEncode.LEncodeSmartBuild(work, plan, LScoutStream.LScoutStreamRead(work.LWorkSourcePath));
    }

    internal static IReadOnlyList<LEncodeStage> TBridgeMissingBuild(LWorkItem work) =>
        LEncode.LEncodeSmartBuild(
            work,
            new LBridgePlan(
                LBridgeOutcome.LBridgeOutcomeSmart,
                new LBridgeSpan(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)),
                null,
                new LBridgeSpan(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)),
                null),
            TSourceStreamCreate("h264"));

    internal static LWorkItem TBridgeCropCreate(string source, string output)
    {
        LWorkItem work = TWorkCreate(
            LWorkKind.LWorkKindSplit, source, output,
            TOutputCreate(videoMode: "Smart", audioMode: "Copy"),
            TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30),
            crop: new LWorkCrop(20, 0, 0, 0, 0, false, false));
        work.LWorkSourceMedia = new LWorkMedia(1920, 1080, 30, 30_000, true) { LWorkMediaCodec = "h264" };
        return work;
    }

    internal static LBridgeSpan TBridgeSpanCreate(double origin, double end, double? decodeEnd = null) =>
        new(
            TimeSpan.FromSeconds(origin),
            TimeSpan.FromSeconds(end),
            TimeSpan.FromSeconds(decodeEnd ?? end));
}
