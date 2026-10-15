namespace Cadroue.ShellEngine;

public static class LUsherEnvironmentEngine
{
    public static Func<string, string> LUsherProgramSeam { get; set; } = lName => lName;

    // The full path; a path the system cannot resolve, or any path while unattached, stays as given.
    public static Func<string, string> LUsherPathSeam { get; set; } = lPath => lPath;
}
