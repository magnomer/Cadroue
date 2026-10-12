namespace Cadroue.Core;

public sealed record LRigDepot(
    Func<string> LRigDepotAudio,
    Func<string> LRigDepotMerge,
    Func<string> LRigDepotBridge,
    Func<string> LRigDepotPass,
    Func<string> LRigDepotRoot);
