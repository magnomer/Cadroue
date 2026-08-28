using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class SmartEncodingCommandTests
{
    private static readonly string SmartSource = Path.Combine("input media", "source clip.mov");
    private static readonly string SmartOutput = Path.Combine("output media", "smart clip.mp4");

    [Fact]
    public void SmartPlan_EmitsVideoOnlyBridgesSeparateAudioAndCopyJoin()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        // head, middle, tail (each followed by its MPEG-TS join piece) + audio + join.
        Assert.Equal(8, stages.Count);

        LEncodeStage head = stages[IndexOfLabel(stages, "Encoding head bridge")];
        IReadOnlyList<string> headTokens = CommandTokens.Read(head.LEncodeStageArguments);
        Assert.True(head.LEncodeStageTemporary);
        Assert.Equal("libx264", CommandTokens.ValueAfter(headTokens, "-c:v"));
        Assert.DoesNotContain("-qp", headTokens);
        Assert.Equal("high", CommandTokens.ValueAfter(headTokens, "-profile:v"));
        Assert.Equal("yuv420p", CommandTokens.ValueAfter(headTokens, "-pix_fmt"));
        Assert.Equal("5000000", CommandTokens.ValueAfter(headTokens, "-b:v"));
        Assert.Contains("-an", headTokens);
        Assert.DoesNotContain("-c:a", headTokens);
        Assert.Equal("passthrough", CommandTokens.ValueAfter(headTokens, "-fps_mode"));
        Assert.DoesNotContain("-r", headTokens);
        Assert.Equal("30000", CommandTokens.ValueAfter(headTokens, "-video_track_timescale"));
        Assert.Equal("10", CommandTokens.ValueAfter(headTokens, "-ss"));
        Assert.Equal("2", CommandTokens.ValueAfter(headTokens, "-t"));
        Assert.Equal(".mov", Path.GetExtension(head.LEncodeStagePath));

        LEncodeStage middle = stages[IndexOfLabel(stages, "Copying middle")];
        IReadOnlyList<string> middleTokens = CommandTokens.Read(middle.LEncodeStageArguments);
        Assert.True(middle.LEncodeStageTemporary);
        Assert.Equal("copy", CommandTokens.ValueAfter(middleTokens, "-c:v"));
        Assert.Contains("-an", middleTokens);
        Assert.DoesNotContain("-c:a", middleTokens);
        Assert.Equal("12", CommandTokens.ValueAfter(middleTokens, "-ss"));
        Assert.Equal("16", CommandTokens.ValueAfter(middleTokens, "-t"));
        Assert.Equal("0", CommandTokens.ValueAfter(middleTokens, "-copypriorss"));
        Assert.Equal("30000", CommandTokens.ValueAfter(middleTokens, "-video_track_timescale"));
        Assert.Equal(".mov", Path.GetExtension(middle.LEncodeStagePath));

        LEncodeStage tail = stages[IndexOfLabel(stages, "Encoding tail bridge")];
        IReadOnlyList<string> tailTokens = CommandTokens.Read(tail.LEncodeStageArguments);
        Assert.True(tail.LEncodeStageTemporary);
        Assert.Equal("libx264", CommandTokens.ValueAfter(tailTokens, "-c:v"));
        Assert.Equal("high", CommandTokens.ValueAfter(tailTokens, "-profile:v"));
        Assert.Equal("yuv420p", CommandTokens.ValueAfter(tailTokens, "-pix_fmt"));
        Assert.DoesNotContain("-qp", tailTokens);
        Assert.Contains("-an", tailTokens);
        Assert.Equal("30000", CommandTokens.ValueAfter(tailTokens, "-video_track_timescale"));
        Assert.Equal(".mov", Path.GetExtension(tail.LEncodeStagePath));

        LEncodeStage audio = stages[IndexOfLabel(stages, "Copying audio")];
        IReadOnlyList<string> audioTokens = CommandTokens.Read(audio.LEncodeStageArguments);
        Assert.True(audio.LEncodeStageTemporary);
        Assert.Equal("Copying audio", audio.LEncodeStageLabel);
        Assert.Contains("-vn", audioTokens);
        Assert.Equal("0:a:0", CommandTokens.ValueAfter(audioTokens, "-map"));
        Assert.Equal("copy", CommandTokens.ValueAfter(audioTokens, "-c:a"));
        Assert.Equal("10", CommandTokens.ValueAfter(audioTokens, "-ss"));
        Assert.Equal("20", CommandTokens.ValueAfter(audioTokens, "-t"));
        Assert.Equal(".mov", Path.GetExtension(audio.LEncodeStagePath));
        Assert.DoesNotContain("-video_track_timescale", audioTokens);

        LEncodeStage mux = stages[^1];
        IReadOnlyList<string> muxTokens = CommandTokens.Read(mux.LEncodeStageArguments);
        Assert.False(mux.LEncodeStageTemporary);
        Assert.Equal(work.LWorkOutputPath, mux.LEncodeStagePath);
        Assert.Equal(LWorkStage.LWorkStageMux, mux.LEncodeStageKind);
        Assert.Contains("concat", muxTokens);
        Assert.Equal("copy", CommandTokens.ValueAfter(muxTokens, "-c"));
        Assert.Equal("30000", CommandTokens.ValueAfter(muxTokens, "-video_track_timescale"));
        Assert.DoesNotContain("-avoid_negative_ts", muxTokens);
        Assert.Equal(1, CommandTokens.Count(muxTokens, "1:a"));
        Assert.Equal(work.LWorkOutputPath, muxTokens[^1]);

        string joinPath = CommandTokens.ValueAfter(muxTokens, "-i");
        string joinList = File.ReadAllText(joinPath);
        int headOrder = joinList.IndexOf(".head", StringComparison.Ordinal);
        int middleOrder = joinList.IndexOf(".middle", StringComparison.Ordinal);
        int tailOrder = joinList.IndexOf(".tail", StringComparison.Ordinal);
        Assert.True(headOrder >= 0 && headOrder < middleOrder && middleOrder < tailOrder);
    }

    [Fact]
    public void MatroskaFallback_UsesMatroskaForEveryTemporaryStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work,
            LBridgeOutcome.LBridgeOutcomeSmart,
            (10, 30),
            (10, 12),
            (12, 28),
            (28, 30),
            intermediateExtension: ".mkv");

        // The head/middle/tail spans and the audio stage keep the requested Matroska
        // container; the per-piece MPEG-TS remux is a separate join requirement.
        IReadOnlyList<LEncodeStage> matroskaStages = stages
            .Where(stage => stage.LEncodeStageTemporary
                && stage.LEncodeStageLabel != "Preparing bridge piece")
            .ToArray();
        Assert.Equal(4, matroskaStages.Count);
        Assert.Single(matroskaStages, stage => stage.LEncodeStageLabel == "Copying audio");
        Assert.All(
            matroskaStages,
            stage => Assert.Equal(".mkv", Path.GetExtension(stage.LEncodeStagePath)));
        Assert.All(
            matroskaStages,
            stage => Assert.DoesNotContain(
                "-video_track_timescale",
                CommandTokens.Read(stage.LEncodeStageArguments)));

        IReadOnlyList<LEncodeStage> pieceStages = stages
            .Where(stage => stage.LEncodeStageLabel == "Preparing bridge piece")
            .ToArray();
        Assert.Equal(3, pieceStages.Count);
        Assert.All(
            pieceStages,
            stage => Assert.Equal(".ts", Path.GetExtension(stage.LEncodeStagePath)));
    }

    [Fact]
    public void KeyedOriginPlan_OmitsHeadBridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 28), (28, 30));

        // middle, middle piece, tail, tail piece, audio, join (no head).
        Assert.Equal(6, stages.Count);
        Assert.Equal("Copying middle", stages[0].LEncodeStageLabel);
        Assert.Equal(LWorkStage.LWorkStageMux, stages[^1].LEncodeStageKind);
    }

    [Fact]
    public void CopiedMiddle_StopsBeforeFollowingKeyframeDecodeTimestamp()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartDecodedStagesBuild(
            work,
            (10, 30),
            (10, 12),
            (12, 28, 27.933),
            (28, 30),
            TEncodeCommand.SourceStreamCreate("h264"));
        IReadOnlyList<string> middleTokens = CommandTokens.Read(
            stages[IndexOfLabel(stages, "Copying middle")].LEncodeStageArguments);

        Assert.Equal("12", CommandTokens.ValueAfter(middleTokens, "-ss"));
        Assert.Equal("15.933", CommandTokens.ValueAfter(middleTokens, "-t"));
        Assert.Equal("0", CommandTokens.ValueAfter(middleTokens, "-copypriorss"));
    }

    [Fact]
    public void WholeCopyableVideoWithCopyAudio_UsesOrdinarySinglePassCopy()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        // Both boundaries are keyframes. Smart must use the same simultaneous stream
        // copy timing as Copy instead of manufacturing separate Matroska timelines.
        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 30), null));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal("Copying", stage.LEncodeStageLabel);
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal("10", CommandTokens.ValueAfter(tokens, "-ss"));
        Assert.Equal("20", CommandTokens.ValueAfter(tokens, "-t"));
        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:a"));
        Assert.Equal("make_zero", CommandTokens.ValueAfter(tokens, "-avoid_negative_ts"));
        Assert.Equal("30000", CommandTokens.ValueAfter(tokens, "-video_track_timescale"));
    }

    [Fact]
    public void NoCopyableMiddle_UsesSameMp4TimescaleAsHybridAndDirectSmart()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeWhole, (10, 11), null, null, null));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.Equal("Encoding", stage.LEncodeStageLabel);
        Assert.Equal("30000", CommandTokens.ValueAfter(tokens, "-video_track_timescale"));
        Assert.Equal(work.LWorkOutputPath, tokens[^1]);
    }

    [Fact]
    public void WholeCopyableVideoWithEncodeAudio_UsesSinglePassVideoCopyAndAudioEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioCodec: "aac", audioMode: "Encode");

        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 30), null));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.Equal("Copying", stage.LEncodeStageLabel);
        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.Equal("aac", CommandTokens.ValueAfter(tokens, "-c:a"));
        Assert.DoesNotContain("concat", tokens);
    }

    [Fact]
    public void HevcSource_UsesMatchedLibx265Bridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "hevc");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 30), null,
            TEncodeCommand.SourceStreamCreate("hevc", profile: "Main"));

        IReadOnlyList<string> headTokens = CommandTokens.Read(stages[0].LEncodeStageArguments);
        Assert.Equal("libx265", CommandTokens.ValueAfter(headTokens, "-c:v"));
        Assert.Equal("main", CommandTokens.ValueAfter(headTokens, "-profile:v"));
        Assert.DoesNotContain("lossless=1", stages[0].LEncodeStageArguments);
    }

    [Fact]
    public void HevcSourceWithHead_InsertsLeadingNormalizeBetweenMiddleAndJoin()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "hevc");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30),
            TEncodeCommand.SourceStreamCreate("hevc", profile: "Main"));

        int middleOrder = IndexOfLabel(stages, "Copying middle");
        int adjustOrder = IndexOfLabel(stages, "Normalizing splice");
        int joinOrder = IndexOfLabel(stages, "Joining bridges");
        Assert.True(middleOrder >= 0 && adjustOrder == middleOrder + 1 && adjustOrder < joinOrder);

        LEncodeStage adjust = stages[adjustOrder];
        Assert.Equal(LWorkStage.LWorkStageSplice, adjust.LEncodeStageKind);
        Assert.True(adjust.LEncodeStageTemporary);
        Assert.EndsWith(".middle.mov", adjust.LEncodeStagePath, StringComparison.Ordinal);
        Assert.Equal(string.Empty, adjust.LEncodeStageArguments);
    }

    [Fact]
    public void H264Source_HasNoLeadingNormalizeStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        Assert.DoesNotContain(stages, stage => stage.LEncodeStageKind == LWorkStage.LWorkStageSplice);
    }

    [Fact]
    public void HevcHeadlessPlan_HasNoLeadingNormalizeStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "hevc");

        // No head bridge: the copied middle is first, so a decoder discards its leading
        // pictures at the stream start and no neutralization is required.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 28), (28, 30),
            TEncodeCommand.SourceStreamCreate("hevc", profile: "Main"));

        Assert.DoesNotContain(stages, stage => stage.LEncodeStageKind == LWorkStage.LWorkStageSplice);
    }

    private static int IndexOfLabel(IReadOnlyList<LEncodeStage> stages, string label)
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

    [Fact]
    public void Vp9Source_UsesMatchedLibvpxBridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "vp9");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 30), null,
            TEncodeCommand.SourceStreamCreate("vp9", profile: "Profile 0"));

        IReadOnlyList<string> headTokens = CommandTokens.Read(stages[0].LEncodeStageArguments);
        Assert.Equal("libvpx-vp9", CommandTokens.ValueAfter(headTokens, "-c:v"));
    }

    [Fact]
    public void ProresSource_UsesMatchedProresBridge()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "prores");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 30), null,
            TEncodeCommand.SourceStreamCreate("prores", profile: "Standard"));

        IReadOnlyList<string> headTokens = CommandTokens.Read(stages[0].LEncodeStageArguments);
        Assert.Equal("prores_ks", CommandTokens.ValueAfter(headTokens, "-c:v"));
    }

    [Fact]
    public void UnsupportedCodecWithBridges_ProducesNoStages()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "mpeg2video");

        // A boundary re-encode is required (head + tail) but no encoder maps to mpeg2video:
        // smart encoding fails outright rather than mismatching the copied middle.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30),
            TEncodeCommand.SourceStreamCreate("mpeg2video", profile: "Main"));

        Assert.Empty(stages);
    }

    [Fact]
    public void UnsupportedCodecWholeCopyable_StillCopiesWithoutAnEncoder()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, "mpeg2video");

        // No head/tail: the whole video is stream-copied, so the unmapped codec never matters.
        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), null, (10, 30), null,
            TEncodeCommand.SourceStreamCreate("mpeg2video", profile: "Main")));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.Equal("Copying", stage.LEncodeStageLabel);
        Assert.Equal("copy", CommandTokens.ValueAfter(tokens, "-c:v"));
    }

    [Fact]
    public void CopyableAudio_StaysSingleCopyRegionInItsOwnStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioCodec: "pcm_s16le");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        // Video is split into head/middle/tail; none of them touch audio.
        string[] videoLabels = { "Encoding head bridge", "Copying middle", "Encoding tail bridge" };
        foreach (string videoLabel in videoLabels)
        {
            LEncodeStage videoStage = stages[IndexOfLabel(stages, videoLabel)];
            IReadOnlyList<string> videoTokens = CommandTokens.Read(videoStage.LEncodeStageArguments);
            Assert.Contains("-an", videoTokens);
            Assert.DoesNotContain("-c:a", videoTokens);
        }

        // Audio is one continuous stream-copy region cut over the whole requested interval.
        // It retains source-relative packet timestamps so delayed tracks stay delayed.
        IReadOnlyList<string> audioTokens = CommandTokens.Read(stages[^2].LEncodeStageArguments);
        Assert.Equal("copy", CommandTokens.ValueAfter(audioTokens, "-c:a"));
        Assert.Equal("10", CommandTokens.ValueAfter(audioTokens, "-ss"));
        Assert.Equal("20", CommandTokens.ValueAfter(audioTokens, "-t"));
        Assert.DoesNotContain("-avoid_negative_ts", audioTokens);

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.DoesNotContain("-c:a", muxTokens);
    }

    [Fact]
    public void EncodeAudioMode_ReEncodesTheContinuousIntervalInItsOwnStage()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioCodec: "aac", audioMode: "Encode");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> audioTokens = CommandTokens.Read(stages[^2].LEncodeStageArguments);
        Assert.NotEqual("copy", CommandTokens.ValueAfter(audioTokens, "-c:a"));
        Assert.Equal("aac", CommandTokens.ValueAfter(audioTokens, "-c:a"));
        // Still one continuous audio region over the requested interval, not per-region pieces.
        Assert.Equal("0:a:0", CommandTokens.ValueAfter(audioTokens, "-map"));
        Assert.Equal("20", CommandTokens.ValueAfter(audioTokens, "-t"));
    }

    [Fact]
    public void AllAudioTracks_ExtractsEveryTrackAndCarriesThemThroughTheJoin()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioStream: "Include all audio tracks");

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> audioTokens = CommandTokens.Read(stages[^2].LEncodeStageArguments);
        Assert.Equal("0:a", CommandTokens.ValueAfter(audioTokens, "-map"));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Equal(1, CommandTokens.Count(muxTokens, "1:a"));
    }

    [Fact]
    public void FinalMux_MapsVideoAndAudioTracksOnce()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Equal(2, CommandTokens.Count(muxTokens, "-map"));
        Assert.Equal(1, CommandTokens.Count(muxTokens, "0:v:0"));
        Assert.Equal(1, CommandTokens.Count(muxTokens, "1:a"));
    }

    [Fact]
    public void SilentSource_JoinsVideoWithoutAnyAudioTrack()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(
            SmartSource, SmartOutput, audioCodec: "", sampleRate: 0);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        // head, middle, tail (each with its join piece), join — no audio stage.
        Assert.Equal(7, stages.Count);
        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Contains("-an", muxTokens);
        Assert.Equal(0, CommandTokens.Count(muxTokens, "1:a"));
        Assert.Equal("copy", CommandTokens.ValueAfter(muxTokens, "-c"));
    }

    [Fact]
    public void WholeOutcome_EmitsSingleNormalEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput, copyMode: false);

        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeWhole, (10, 30), null, null, null));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal("libx264", CommandTokens.ValueAfter(tokens, "-c:v"));
        Assert.NotEqual("copy", CommandTokens.ValueAfter(tokens, "-c"));
    }

    [Fact]
    public void InvalidOutcome_EmitsSingleEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        Assert.Single(TEncodeCommand.SmartStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeInvalid, (30, 10), null, null, null));
    }

    [Fact]
    public void ProcessingPresent_DefersToWholeIntervalEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartCropWorkCreate(SmartSource, SmartOutput);

        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartResolveBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30)));
        IReadOnlyList<string> tokens = CommandTokens.Read(stage.LEncodeStageArguments);

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal("libx264", CommandTokens.ValueAfter(tokens, "-c:v"));
    }

    [Fact]
    public void NonKeyframeBoundsSplit_ProducesHybridStageList()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        // Interval is [10, 30]; interior keyframes at 12 and 28 align with neither bound.
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartBridgeResolve(work, 12, 28);

        Assert.Equal(8, stages.Count);
        Assert.Equal("Encoding head bridge", stages[0].LEncodeStageLabel);
        Assert.True(IndexOfLabel(stages, "Copying middle") >= 0);
        Assert.True(IndexOfLabel(stages, "Encoding tail bridge") >= 0);

        IReadOnlyList<string> middleTokens = CommandTokens.Read(
            stages[IndexOfLabel(stages, "Copying middle")].LEncodeStageArguments);
        Assert.Equal("copy", CommandTokens.ValueAfter(middleTokens, "-c:v"));
        Assert.Equal("12", CommandTokens.ValueAfter(middleTokens, "-ss"));
        Assert.Equal("16", CommandTokens.ValueAfter(middleTokens, "-t"));

        IReadOnlyList<string> muxTokens = CommandTokens.Read(stages[^1].LEncodeStageArguments);
        Assert.Equal(LWorkStage.LWorkStageMux, stages[^1].LEncodeStageKind);
        Assert.Contains("concat", muxTokens);
    }

    [Fact]
    public void KeyframeStarvedSplit_FallsBackToWholeIntervalEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        // No usable interior keyframe within [10, 30]: whole-interval fallback.
        LEncodeStage stage = Assert.Single(TEncodeCommand.SmartBridgeResolve(work));

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal(LWorkStage.LWorkStageEncode, stage.LEncodeStageKind);
        Assert.DoesNotContain("concat", CommandTokens.Read(stage.LEncodeStageArguments));
    }

    [Fact]
    public void CleanCopyableCut_UsesHybridSmartPlan()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.SmartWorkCreate(SmartSource, SmartOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.SmartResolveBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart, (10, 30), (10, 12), (12, 28), (28, 30));

        Assert.Equal(8, stages.Count);
        Assert.True(stages[0].LEncodeStageTemporary);
        Assert.Contains("concat", CommandTokens.Read(stages[^1].LEncodeStageArguments));
    }
}
