namespace Convention.Tests;

internal static class TAuditPuritySetting
{
    public const int TAuditGeneration = 21;

    public static readonly string[] TAuditPurityRing =
    [
        "Cadroue.Core",
        "Cadroue.Application",
        "Cadroue.ShellEngine",
        "Cadroue.Conduct",
    ];

    public static readonly string[] TAuditPurityFrame =
    [
        "System",
        "System.Collections.Generic",
        "System.Linq",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Globalization",
        "System.Threading",
        "System.Threading.Tasks",
        "System.Diagnostics.CodeAnalysis",
        "System.Runtime.CompilerServices",
    ];

    public static readonly IReadOnlyDictionary<string, string[]> TAuditPurityExtra = new Dictionary<string, string[]>
    {
        ["Cadroue.Core"] = [],
        ["Cadroue.Application"] = [],
        ["Cadroue.ShellEngine"] = ["System.Runtime.ExceptionServices"],
        ["Cadroue.Conduct"] = ["System.Runtime.ExceptionServices"],
    };

    public static readonly string[] TAuditEavesdroppingMember =
    [
        "System.DateTime.Now",
        "System.DateTime.UtcNow",
        "System.DateTime.Today",
        "System.DateTimeOffset.Now",
        "System.DateTimeOffset.UtcNow",
        "System.Environment",
        "System.Guid.NewGuid",
        "System.Random",
        "System.Console",
        "System.IO.File",
        "System.IO.Directory",
        "System.IO.Path",
        "System.Diagnostics.Process",
        "System.Diagnostics.Stopwatch",
    ];

    public static readonly IReadOnlyDictionary<string, int> TAuditPurityCeiling = new Dictionary<string, int>
    {
        ["Eavesdropping:Cadroue.Application>System.DateTimeOffset.Now"] = 5,
        ["Eavesdropping:Cadroue.Application>System.DateTimeOffset.UtcNow"] = 1,
        ["Eavesdropping:Cadroue.Application>System.IO.File"] = 1,
        ["Eavesdropping:Cadroue.Application>System.IO.Path"] = 44,
        ["Eavesdropping:Cadroue.Core>System.DateTimeOffset.Now"] = 2,
        ["Eavesdropping:Cadroue.Core>System.Diagnostics.Process"] = 5,
        ["Eavesdropping:Cadroue.Core>System.Guid.NewGuid"] = 2,
        ["Eavesdropping:Cadroue.Core>System.IO.Path"] = 20,
        ["Eavesdropping:Cadroue.ShellEngine>System.DateTimeOffset.Now"] = 8,
        ["Eavesdropping:Cadroue.ShellEngine>System.Diagnostics.Process"] = 0,
        ["Eavesdropping:Cadroue.ShellEngine>System.Diagnostics.Stopwatch"] = 9,
        ["Eavesdropping:Cadroue.ShellEngine>System.Environment"] = 4,
        ["Eavesdropping:Cadroue.ShellEngine>System.Guid.NewGuid"] = 3,
        ["Eavesdropping:Cadroue.ShellEngine>System.IO.Directory"] = 6,
        ["Eavesdropping:Cadroue.ShellEngine>System.IO.File"] = 46,
        ["Eavesdropping:Cadroue.ShellEngine>System.IO.Path"] = 67,
        ["Foraging:Cadroue.Application>System.Buffers.Binary"] = 3,
        ["Foraging:Cadroue.Application>System.Collections.ObjectModel"] = 17,
        ["Foraging:Cadroue.Application>System.IO"] = 72,
        ["Foraging:Cadroue.Application>System.Security.Cryptography"] = 2,
        ["Foraging:Cadroue.Application>System.Text.Json"] = 22,
        ["Foraging:Cadroue.Core>System.Collections.Concurrent"] = 5,
        ["Foraging:Cadroue.Core>System.Collections.ObjectModel"] = 2,
        ["Foraging:Cadroue.Core>System.ComponentModel"] = 3,
        ["Foraging:Cadroue.Core>System.Diagnostics"] = 8,
        ["Foraging:Cadroue.Core>System.IO"] = 41,
        ["Foraging:Cadroue.Core>System.Runtime.InteropServices"] = 14,
        ["Foraging:Cadroue.Core>System.Security"] = 1,
        ["Foraging:Cadroue.Core>System.Security.Cryptography"] = 9,
        ["Foraging:Cadroue.Core>System.Text.Json.Serialization"] = 26,
        ["Foraging:Cadroue.ShellEngine>System.Collections.Concurrent"] = 27,
        ["Foraging:Cadroue.ShellEngine>System.ComponentModel"] = 3,
        ["Foraging:Cadroue.ShellEngine>System.Diagnostics"] = 11,
        ["Foraging:Cadroue.ShellEngine>System.IO"] = 172,
        ["Foraging:Cadroue.ShellEngine>System.Reflection"] = 3,
        ["Foraging:Cadroue.ShellEngine>System.Runtime.InteropServices"] = 0,
        ["Foraging:Cadroue.ShellEngine>System.Text.Json"] = 43,
        ["Foraging:Cadroue.ShellEngine>System.Timers"] = 6,
    };

    public static readonly string[] TAuditPurityExempt = [];
}
