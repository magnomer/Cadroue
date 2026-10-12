namespace Cadroue.ShellEngine;

public static class LUsherTextEngine
{
    public static Func<string, string> LUsherNameSeam { get; set; } = _ => string.Empty;

    public static Func<string, string> LUsherStemSeam { get; set; } = _ => string.Empty;

    public static Func<string, string> LUsherExtensionSeam { get; set; } = _ => string.Empty;
}
