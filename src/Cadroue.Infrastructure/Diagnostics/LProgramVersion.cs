using System.Reflection;

namespace Cadroue.Infrastructure;

public static class LProgramVersion
{
    public static string LProgramVersionRead()
    {
        Assembly lAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        string lVersion = lAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? lAssembly.GetName().Version?.ToString()
            ?? string.Empty;
        int lBuildMark = lVersion.IndexOf('+');
        return lBuildMark < 0 ? lVersion : lVersion[..lBuildMark];
    }
}
