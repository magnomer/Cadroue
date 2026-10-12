namespace Cadroue.ShellEngine;

public static class LUsherEngine
{
    public static Func<string, string?, string?> LUsherRevealSeam { get; set; } = (_, _) => null;

    public static Func<string, string?> LUsherExplorerSeam { get; set; } = _ => null;

    public static Func<string, string?> LUsherLinkSeam { get; set; } = _ => null;
}
