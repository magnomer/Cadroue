using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
    public static Func<Guid, string>? LMessengerTitleSource { get; set; }

    public static Func<IReadOnlyList<LWorkItem>, Guid, Guid, LCartographerPlanRecord?, int>? LMessengerRouteSource { get; set; }

    public static Func<LScheduleContract?>? LMessengerScheduleSource { get; set; }

    public static Func<Guid, string, Guid, bool>? LMessengerDeliverSource { get; set; }

    public static Action<IReadOnlyList<string>>? LMessengerDrainSource { get; set; }

    private static string LMessengerTitleRead(Guid lMessengerRelaySource) =>
        LMessengerTitleSource?.Invoke(lMessengerRelaySource) ?? string.Empty;

    private static int LMessengerDispatch(
        IReadOnlyList<LWorkItem> lMessengerItems,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        LCartographerPlanRecord? lMessengerPlan = null) =>
        LMessengerRouteSource?.Invoke(lMessengerItems, lMessengerRelayTarget, lMessengerRelaySource, lMessengerPlan) ?? 0;

    // Route a schedule-mutating action onto the post thread the worklist writes on, falling back to
    // inline when no post owner is wired.
    private static void LMessengerDefer(Action lMessengerAction)
    {
        if (LStation.LStationPost is { } lMessengerPost)
        {
            lMessengerPost(lMessengerAction);
            return;
        }

        lMessengerAction();
    }
}
