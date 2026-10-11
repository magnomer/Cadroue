namespace Cadroue.ShellEngine;

public static class LDepotEngine
{
    public static Func<string> LDepotAudioSeam { get; set; } = () => "";

    public static Func<string> LDepotMergeSeam { get; set; } = () => "";

    public static Func<string> LDepotBridgeSeam { get; set; } = () => "";

    public static Func<string> LDepotPassSeam { get; set; } = () => "";

    public static Func<string> LDepotRootSeam { get; set; } = () => "";
}
