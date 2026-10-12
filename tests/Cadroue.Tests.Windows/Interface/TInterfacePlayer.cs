using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.Tests.Windows;

internal static class TInterfacePlayer
{
    internal static QPlayer TPlayerCreate() => new();
    internal static bool TPlayerAccurateSet(QPlayer player) => player.QPlayerAccurateSet();
    internal static void TPlayerAccurateReset(QPlayer player) => player.QPlayerAccurateReset();
    internal static void TPlayerRendererSet(QPlayer player, bool pending) => player.QPlayerRendererSet(pending);
    internal static bool TPlayerSeekCommit(QPlayer player, int milliseconds) => player.QPlayerSeekCommit(milliseconds);
    internal static Task TPlayerOpenStart(QPlayer player, string path) => player.QPlayerOpenStart(path);
    internal static bool TPlayerFilterApply(QPlayer player, string filter) => player.QPlayerFilterApply(filter);
    internal static void TPlayerEngineSet(QPlayer player, QPlayerSeam? seam) => player.QPlayerEngineSet(seam);
    internal static QPlayerSeam TPlayerSeamCreate(
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
    internal static void TPlayerAppliedReset(QPlayer player) => player.QPlayerAppliedReset();
    internal static void TPlayerEndSet(QPlayer player, TimeSpan? end) => player.QPlayerEndSet(end);

    internal static void TPlayerSeek(QPlayer player, TimeSpan position) => player.QPlayerSeek(position);
    internal static TimeSpan TPlayerTimeRead(QPlayer player) => player.QPlayerTimeRead();
    internal static bool TPlayerEndedRead(QPlayer player) => player.QPlayerEndedRead();

    internal static void TPlayerPreviewApply(QPlayer player, LPreviewState state) =>
        player.QPlayerPreviewApply(state, "test");
    internal static void TPlayerAudioApply(QPlayer player, string audio) => player.QPlayerAudioApply(audio);
}
