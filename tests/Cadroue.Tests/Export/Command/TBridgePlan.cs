using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

[Collection("EncodeCommand")]
public sealed class TBridgePlan
{
    [Fact]
    public void SmartPlan_EmitsVideoOnlyBridgesSeparateAudioAndCopyJoin()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart,
            TEncodeCommand.TBridgeSpanCreate(10, 30),
            TEncodeCommand.TBridgeSpanCreate(10, 12),
            TEncodeCommand.TBridgeSpanCreate(12, 28),
            TEncodeCommand.TBridgeSpanCreate(28, 30));

        Assert.Equal(8, stages.Count);

        LEncodeStage head = stages[TEncodeCommand.TBridgeLabelFind(stages, "Encoding head bridge")];
        IReadOnlyList<string> headTokens = TEncodeToken.TEncodeTokenRead(head.LEncodeStageArguments);
        Assert.True(head.LEncodeStageTemporary);
        Assert.Equal("libx264", TEncodeToken.TEncodeOptionRead(headTokens, "-c:v"));
        Assert.DoesNotContain("-qp", headTokens);
        Assert.Equal("high", TEncodeToken.TEncodeOptionRead(headTokens, "-profile:v"));
        Assert.Equal("yuv420p", TEncodeToken.TEncodeOptionRead(headTokens, "-pix_fmt"));
        Assert.Equal("5000000", TEncodeToken.TEncodeOptionRead(headTokens, "-b:v"));
        Assert.Contains("-an", headTokens);
        Assert.DoesNotContain("-c:a", headTokens);
        Assert.Equal("passthrough", TEncodeToken.TEncodeOptionRead(headTokens, "-fps_mode"));
        Assert.DoesNotContain("-r", headTokens);
        Assert.Equal("30000", TEncodeToken.TEncodeOptionRead(headTokens, "-video_track_timescale"));
        Assert.Equal("10", TEncodeToken.TEncodeOptionRead(headTokens, "-ss"));
        Assert.Equal("2", TEncodeToken.TEncodeOptionRead(headTokens, "-t"));
        Assert.Equal(".mov", Path.GetExtension(head.LEncodeStagePath));

        LEncodeStage middle = stages[TEncodeCommand.TBridgeLabelFind(stages, "Copying middle")];
        IReadOnlyList<string> middleTokens = TEncodeToken.TEncodeTokenRead(middle.LEncodeStageArguments);
        Assert.True(middle.LEncodeStageTemporary);
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(middleTokens, "-c:v"));
        Assert.Contains("-an", middleTokens);
        Assert.DoesNotContain("-c:a", middleTokens);
        Assert.Equal("12", TEncodeToken.TEncodeOptionRead(middleTokens, "-ss"));
        Assert.Equal("16", TEncodeToken.TEncodeOptionRead(middleTokens, "-t"));
        Assert.Equal("0", TEncodeToken.TEncodeOptionRead(middleTokens, "-copypriorss"));
        Assert.Equal("30000", TEncodeToken.TEncodeOptionRead(middleTokens, "-video_track_timescale"));
        Assert.Equal(".mov", Path.GetExtension(middle.LEncodeStagePath));

        LEncodeStage tail = stages[TEncodeCommand.TBridgeLabelFind(stages, "Encoding tail bridge")];
        IReadOnlyList<string> tailTokens = TEncodeToken.TEncodeTokenRead(tail.LEncodeStageArguments);
        Assert.True(tail.LEncodeStageTemporary);
        Assert.Equal("libx264", TEncodeToken.TEncodeOptionRead(tailTokens, "-c:v"));
        Assert.Equal("high", TEncodeToken.TEncodeOptionRead(tailTokens, "-profile:v"));
        Assert.Equal("yuv420p", TEncodeToken.TEncodeOptionRead(tailTokens, "-pix_fmt"));
        Assert.DoesNotContain("-qp", tailTokens);
        Assert.Contains("-an", tailTokens);
        Assert.Equal("30000", TEncodeToken.TEncodeOptionRead(tailTokens, "-video_track_timescale"));
        Assert.Equal(".mov", Path.GetExtension(tail.LEncodeStagePath));

        LEncodeStage audio = stages[TEncodeCommand.TBridgeLabelFind(stages, "Copying audio")];
        IReadOnlyList<string> audioTokens = TEncodeToken.TEncodeTokenRead(audio.LEncodeStageArguments);
        Assert.True(audio.LEncodeStageTemporary);
        Assert.Equal("Copying audio", audio.LEncodeStageLabel);
        Assert.Contains("-vn", audioTokens);
        Assert.Equal("0:a:0", TEncodeToken.TEncodeOptionRead(audioTokens, "-map"));
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(audioTokens, "-c:a"));
        Assert.Equal("10", TEncodeToken.TEncodeOptionRead(audioTokens, "-ss"));
        Assert.Equal("20", TEncodeToken.TEncodeOptionRead(audioTokens, "-t"));
        Assert.Equal(".mov", Path.GetExtension(audio.LEncodeStagePath));
        Assert.DoesNotContain("-video_track_timescale", audioTokens);

        LEncodeStage mux = stages[^1];
        IReadOnlyList<string> muxTokens = TEncodeToken.TEncodeTokenRead(mux.LEncodeStageArguments);
        Assert.False(mux.LEncodeStageTemporary);
        Assert.Equal(work.LWorkOutputPath, mux.LEncodeStagePath);
        Assert.Equal(LWorkStage.LWorkStageMux, mux.LEncodeStageKind);
        Assert.Contains("concat", muxTokens);
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(muxTokens, "-c"));
        Assert.Equal("30000", TEncodeToken.TEncodeOptionRead(muxTokens, "-video_track_timescale"));
        Assert.DoesNotContain("-avoid_negative_ts", muxTokens);
        Assert.Equal(1, TEncodeToken.TEncodeCountRead(muxTokens, "1:a"));
        Assert.Equal(work.LWorkOutputPath, muxTokens[^1]);

        string joinPath = TEncodeToken.TEncodeOptionRead(muxTokens, "-i");
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
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work,
            LBridgeOutcome.LBridgeOutcomeSmart,
            TEncodeCommand.TBridgeSpanCreate(10, 30),
            TEncodeCommand.TBridgeSpanCreate(10, 12),
            TEncodeCommand.TBridgeSpanCreate(12, 28),
            TEncodeCommand.TBridgeSpanCreate(28, 30),
            intermediateExtension: ".mkv");

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
                TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments)));

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
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart,
            TEncodeCommand.TBridgeSpanCreate(10, 30),
            null,
            TEncodeCommand.TBridgeSpanCreate(10, 28),
            TEncodeCommand.TBridgeSpanCreate(28, 30));

        Assert.Equal(6, stages.Count);
        Assert.Equal("Copying middle", stages[0].LEncodeStageLabel);
        Assert.Equal(LWorkStage.LWorkStageMux, stages[^1].LEncodeStageKind);
    }

    [Fact]
    public void CopiedMiddle_StopsBeforeFollowingKeyframeDecodeTimestamp()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);
        IReadOnlyList<LEncodeStage> stages = TEncodeCommand.TBridgeDecodeBuild(
            work,
            TEncodeCommand.TBridgeSpanCreate(10, 30),
            TEncodeCommand.TBridgeSpanCreate(10, 12),
            TEncodeCommand.TBridgeSpanCreate(12, 28, 27.933),
            TEncodeCommand.TBridgeSpanCreate(28, 30),
            TEncodeCommand.TSourceStreamCreate("h264"));
        IReadOnlyList<string> middleTokens = TEncodeToken.TEncodeTokenRead(
            stages[TEncodeCommand.TBridgeLabelFind(stages, "Copying middle")].LEncodeStageArguments);

        Assert.Equal("12", TEncodeToken.TEncodeOptionRead(middleTokens, "-ss"));
        Assert.Equal("15.933", TEncodeToken.TEncodeOptionRead(middleTokens, "-t"));
        Assert.Equal("0", TEncodeToken.TEncodeOptionRead(middleTokens, "-copypriorss"));
    }

    [Fact]
    public void WholeCopyableVideoWithCopyAudio_UsesOrdinarySinglePassCopy()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        LEncodeStage stage = Assert.Single(TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart,
            TEncodeCommand.TBridgeSpanCreate(10, 30),
            null,
            TEncodeCommand.TBridgeSpanCreate(10, 30),
            null));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.False(stage.LEncodeStageTemporary);
        Assert.Equal("Copying", stage.LEncodeStageLabel);
        Assert.DoesNotContain("concat", tokens);
        Assert.Equal("10", TEncodeToken.TEncodeOptionRead(tokens, "-ss"));
        Assert.Equal("20", TEncodeToken.TEncodeOptionRead(tokens, "-t"));
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:a"));
        Assert.Equal("make_zero", TEncodeToken.TEncodeOptionRead(tokens, "-avoid_negative_ts"));
        Assert.Equal("30000", TEncodeToken.TEncodeOptionRead(tokens, "-video_track_timescale"));
    }

    [Fact]
    public void NoCopyableMiddle_UsesSameMp4TimescaleAsHybridAndDirectSmart()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput);

        LEncodeStage stage = Assert.Single(TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeWhole, TEncodeCommand.TBridgeSpanCreate(10, 11), null, null, null));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("Encoding", stage.LEncodeStageLabel);
        Assert.Equal("30000", TEncodeToken.TEncodeOptionRead(tokens, "-video_track_timescale"));
        Assert.Equal(work.LWorkOutputPath, tokens[^1]);
    }

    [Fact]
    public void WholeCopyableVideoWithEncodeAudio_UsesSinglePassVideoCopyAndAudioEncode()
    {
        using var environment = new TEncodeCommand();
        LWorkItem work = TEncodeCommand.TBridgeWorkCreate(
            TEncodeCommand.TBridgeSource, TEncodeCommand.TBridgeOutput,
            audioCodec: "aac", audioMode: "Encode");

        LEncodeStage stage = Assert.Single(TEncodeCommand.TBridgeStagesBuild(
            work, LBridgeOutcome.LBridgeOutcomeSmart,
            TEncodeCommand.TBridgeSpanCreate(10, 30),
            null,
            TEncodeCommand.TBridgeSpanCreate(10, 30),
            null));
        IReadOnlyList<string> tokens = TEncodeToken.TEncodeTokenRead(stage.LEncodeStageArguments);

        Assert.Equal("Copying", stage.LEncodeStageLabel);
        Assert.Equal("copy", TEncodeToken.TEncodeOptionRead(tokens, "-c:v"));
        Assert.Equal("aac", TEncodeToken.TEncodeOptionRead(tokens, "-c:a"));
        Assert.DoesNotContain("concat", tokens);
    }
}
