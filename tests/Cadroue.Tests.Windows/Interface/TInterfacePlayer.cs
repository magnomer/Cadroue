using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfacePlayer
{
    internal static LPlayer TPlayerCreate() => new();
    internal static bool TPlayerAccurateSet(LPlayer player) => player.LPlayerAccurateSet();
    internal static void TPlayerAccurateReset(LPlayer player) => player.LPlayerAccurateReset();
    internal static void TPlayerRendererSet(LPlayer player, bool pending) => player.LPlayerRendererSet(pending);
    internal static bool TPlayerSeekCommit(LPlayer player, int milliseconds) => player.LPlayerSeekCommit(milliseconds);
    internal static Task TPlayerOpenStart(LPlayer player, string path) => player.LPlayerOpenStart(path);
    internal static bool TPlayerFilterApply(LPlayer player, string filter) => player.LPlayerFilterApply(filter);
    internal static void TPlayerEngineSet(LPlayer player, LPlayerSeam? seam) => player.LPlayerEngineSet(seam);
    internal static LPlayerSeam TPlayerSeamCreate(
        Action<string>? open = null,
        Action<string>? filter = null,
        Action<TimeSpan>? seek = null,
        Func<TimeSpan>? time = null,
        Func<bool>? ended = null,
        Action? dispose = null,
        Action<LPreviewApplication>? preview = null) => new(
            open ?? (_ => { }),
            () => { },
            () => { },
            () => { },
            seek ?? (_ => { }),
            _ => { },
            filter ?? (_ => { }),
            _ => { },
            preview ?? (_ => { }),
            () => { },
            time ?? (() => TimeSpan.Zero),
            ended ?? (() => false),
            (_, _) => { },
            dispose ?? (() => { }));
    internal static void TPlayerAppliedReset(LPlayer player) => player.LPlayerAppliedReset();
    internal static void TPlayerEndSet(LPlayer player, TimeSpan? end) => player.LPlayerEndSet(end);

    internal static void TPlayerSeek(LPlayer player, TimeSpan position) => player.LPlayerSeek(position);
    internal static TimeSpan TPlayerTimeRead(LPlayer player) => player.LPlayerTimeRead();
    internal static bool TPlayerEndedRead(LPlayer player) => player.LPlayerEndedRead();

    internal static void TPlayerPreviewApply(LPlayer player, LPreviewState state) =>
        player.LPlayerPreviewApply(state, "test");
    internal static void TPlayerAudioApply(LPlayer player, string audio) => player.LPlayerAudioApply(audio);
}
