namespace Cadroue.Infrastructure;

public static class LRelayStartup
{
    public const string LRelayArgument = "--relay";

    private static string? lRelayStartupPath;

    public static LRelay? LRelayStartupPayload { get; private set; }

    public static LRelay? LRelayPayloadRead()
    {
        LRelay? lRelayPayload = LRelayStartupPayload;
        LRelayStartupPayload = null;
        return lRelayPayload;
    }

    public static void LRelayStartupRead(string[] lStartupArguments)
    {
        for (int lIndex = 0; lIndex < lStartupArguments.Length - 1; lIndex++)
        {
            if (!string.Equals(lStartupArguments[lIndex], LRelayArgument, StringComparison.Ordinal))
            {
                continue;
            }

            string lRelayFilePath = lStartupArguments[lIndex + 1];
            LRelayStartupPayload = LRelayPayload.LRelayPayloadLoad(lRelayFilePath);
            if (LRelayStartupPayload is { } lRelayPayload)
            {
                lRelayStartupPath = lRelayFilePath;
                LTraceLog.LTraceInfoRecord($"Started to receive a relayed '{lRelayPayload.LRelayLayoutKey}' tab");
            }

            return;
        }
    }

    public static void LRelayStartupCommit()
    {
        if (lRelayStartupPath is not { } lRelayFilePath)
        {
            return;
        }

        lRelayStartupPath = null;
        LRelayStore.LRelayFileClear(lRelayFilePath);
        LTraceLog.LTraceInfoRecord("Relayed tab committed; the sender may now close its copy");
    }
}
