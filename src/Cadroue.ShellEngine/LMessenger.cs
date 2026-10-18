using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LMessenger
{
    public static Func<Guid, string>? LMessengerTitleSource { get; set; }

    public static Func<
        IReadOnlyList<LWorkItem>,
        Guid,
        Guid,
        LCartographerPlanDocument?,
        int>? LMessengerRouteSource { get; set; }

    public static Func<LScheduleContract?>? LMessengerScheduleSource { get; set; }

    public static Func<Guid, string, Guid, bool>? LMessengerDeliverSource { get; set; }

    public static Action<Guid, IReadOnlyList<string>>? LMessengerDrainSource { get; set; }

    internal static string LMessengerTitleRead(Guid lMessengerRelaySource) =>
        LMessengerTitleSource?.Invoke(lMessengerRelaySource) ?? string.Empty;

    internal static IReadOnlyDictionary<string, Guid> LMessengerRelayRead(IReadOnlyList<LWorkSource> lMessengerSources)
    {
        var lMessengerRelays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkSource lMessengerSource in lMessengerSources)
        {
            lMessengerRelays[lMessengerSource.LWorkSourcePath] = lMessengerSource.LWorkSourceBatch;
        }

        return lMessengerRelays;
    }

    internal static int LMessengerDispatch(
        IReadOnlyList<LWorkItem> lMessengerItems,
        Guid lMessengerRelayTarget,
        Guid lMessengerRelaySource,
        LCartographerPlanDocument? lMessengerPlan = null) =>
        LMessengerRouteSource?.Invoke(
            lMessengerItems,
            lMessengerRelayTarget,
            lMessengerRelaySource,
            lMessengerPlan) ?? 0;

    internal static void LMessengerDefer(Action lMessengerAction)
    {
        if (LStationEngine.LStationPost is { } lMessengerPost)
        {
            lMessengerPost(lMessengerAction);
            return;
        }

        lMessengerAction();
    }
}
