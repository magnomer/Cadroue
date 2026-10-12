namespace Cadroue.ShellEngine;

public static class LUsherEnvironmentEngine
{
    public static Func<string, string> LUsherProgramSeam { get; set; } = lName => lName;

    public static Func<string, string> LUsherPathSeam { get; set; } = lPath => lPath;
}
