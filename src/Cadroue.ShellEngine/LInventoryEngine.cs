namespace Cadroue.ShellEngine;

public static class LInventoryEngine
{
    public static Func<string, bool> LInventoryFilterSeam { get; set; } = _ => true;

    public static Func<string, IReadOnlyList<string>> LInventoryLayoutSeam { get; set; } = _ => [];

    public static Func<string, IReadOnlyList<int>> LInventorySampleSeam { get; set; } = _ => [];

    public static Action<Action<string>> LInventoryEnrollmentSeam { get; set; } = _ => { };

    public static Action<Action<string>> LInventoryWithdrawalSeam { get; set; } = _ => { };
}
