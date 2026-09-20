namespace Convention.Tests;

internal static class TAuditFrameSetting
{
    public const int TAuditGeneration = 10;

    public static readonly string[] TAuditFramePure =
    [
        "Cadroue.Core",
        "Cadroue.Application",
        "Cadroue.ShellEngine",
        "Cadroue.UIDeportment",
    ];

    public static readonly string[] TAuditFrameAllowed =
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
        "System.Runtime.ExceptionServices",
    ];

    public static readonly string[] TAuditFrameAmbient =
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

    public static readonly IReadOnlyDictionary<string, int> TAuditFrameCeiling = new Dictionary<string, int>
    {
        ["ambient:Cadroue.Application>System.DateTimeOffset.Now"] = 5,
        ["ambient:Cadroue.Application>System.DateTimeOffset.UtcNow"] = 1,
        ["ambient:Cadroue.Application>System.IO.File"] = 1,
        ["ambient:Cadroue.Application>System.IO.Path"] = 9,
        ["ambient:Cadroue.Core>System.DateTimeOffset.Now"] = 2,
        ["ambient:Cadroue.Core>System.Diagnostics.Process"] = 1,
        ["ambient:Cadroue.Core>System.Guid.NewGuid"] = 2,
        ["ambient:Cadroue.Core>System.IO.Path"] = 7,
        ["ambient:Cadroue.ShellEngine>System.DateTimeOffset.Now"] = 3,
        ["ambient:Cadroue.ShellEngine>System.Diagnostics.Process"] = 9,
        ["ambient:Cadroue.ShellEngine>System.Diagnostics.Stopwatch"] = 2,
        ["ambient:Cadroue.ShellEngine>System.Environment"] = 4,
        ["ambient:Cadroue.ShellEngine>System.Guid.NewGuid"] = 3,
        ["ambient:Cadroue.ShellEngine>System.IO.Directory"] = 5,
        ["ambient:Cadroue.ShellEngine>System.IO.File"] = 17,
        ["ambient:Cadroue.ShellEngine>System.IO.Path"] = 24,
        ["ambient:Cadroue.UIDeportment>System.DateTimeOffset.Now"] = 3,
        ["ambient:Cadroue.UIDeportment>System.Diagnostics.Stopwatch"] = 3,
        ["ambient:Cadroue.UIDeportment>System.Environment"] = 7,
        ["ambient:Cadroue.UIDeportment>System.Guid.NewGuid"] = 1,
        ["ambient:Cadroue.UIDeportment>System.IO.Path"] = 6,
        ["frame:Cadroue.Application>System.Buffers.Binary"] = 1,
        ["frame:Cadroue.Application>System.Collections.ObjectModel"] = 2,
        ["frame:Cadroue.Application>System.IO"] = 12,
        ["frame:Cadroue.Application>System.Security.Cryptography"] = 1,
        ["frame:Cadroue.Application>System.Text.Json"] = 2,
        ["frame:Cadroue.Core>System.Collections.Concurrent"] = 1,
        ["frame:Cadroue.Core>System.Collections.ObjectModel"] = 1,
        ["frame:Cadroue.Core>System.ComponentModel"] = 1,
        ["frame:Cadroue.Core>System.Diagnostics"] = 1,
        ["frame:Cadroue.Core>System.IO"] = 7,
        ["frame:Cadroue.Core>System.Runtime.InteropServices"] = 1,
        ["frame:Cadroue.Core>System.Security"] = 1,
        ["frame:Cadroue.Core>System.Security.Cryptography"] = 2,
        ["frame:Cadroue.Core>System.Text.Json.Serialization"] = 4,
        ["frame:Cadroue.ShellEngine>System.Collections.Concurrent"] = 5,
        ["frame:Cadroue.ShellEngine>System.Collections.ObjectModel"] = 1,
        ["frame:Cadroue.ShellEngine>System.ComponentModel"] = 7,
        ["frame:Cadroue.ShellEngine>System.Diagnostics"] = 12,
        ["frame:Cadroue.ShellEngine>System.IO"] = 30,
        ["frame:Cadroue.ShellEngine>System.Reflection"] = 1,
        ["frame:Cadroue.ShellEngine>System.Runtime.InteropServices"] = 1,
        ["frame:Cadroue.ShellEngine>System.Text.Json"] = 5,
        ["frame:Cadroue.ShellEngine>System.Timers"] = 1,
        ["frame:Cadroue.UIDeportment>System.Collections.ObjectModel"] = 2,
        ["frame:Cadroue.UIDeportment>System.ComponentModel"] = 1,
        ["frame:Cadroue.UIDeportment>System.Diagnostics"] = 3,
        ["frame:Cadroue.UIDeportment>System.IO"] = 6,
    };

    public static readonly string[] TAuditFrameWaiver = [];
}
