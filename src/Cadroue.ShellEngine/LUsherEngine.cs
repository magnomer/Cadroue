namespace Cadroue.ShellEngine;

public static class LUsherEngine
{
    public static Func<string?, bool> LUsherFileSeam { get; set; } = _ => false;

    public static Func<string?, bool> LUsherFolderSeam { get; set; } = _ => false;

    public static Func<string, string> LUsherNameSeam { get; set; } = lPath => lPath;

    public static Func<string, string> LUsherStemSeam { get; set; } = lPath => lPath;

    public static Func<string, string> LUsherExtensionSeam { get; set; } = _ => "";

    public static Func<string, string> LUsherProgramSeam { get; set; } = lName => lName;

    public static Func<string, string> LUsherPathSeam { get; set; } = lPath => lPath;

    public static Func<string, string, string?, string?> LUsherRevealSeam { get; set; } = (_, _, _) => null;

    public static Func<string, string, string?> LUsherExplorerSeam { get; set; } = (_, _) => null;

    public static Func<string, string, string?> LUsherLinkSeam { get; set; } = (_, _) => null;
}
