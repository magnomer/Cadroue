namespace Cadroue.ShellEngine;

public static class LUsherDiskEngine
{
    public static Func<string?, bool> LUsherFileSeam { get; set; } = _ => false;

    public static Func<string?, bool> LUsherFolderSeam { get; set; } = _ => false;
}
