namespace Cadroue.ShellEngine;

public static class LInventoryEngine
{
    public static Func<string, bool> LInventoryFilterSeam { get; set; } = _ => true;
}
