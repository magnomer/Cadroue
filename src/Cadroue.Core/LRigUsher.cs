namespace Cadroue.Core;

public sealed record LRigUsher(
    Func<string?, bool> LRigUsherFile,
    Func<string?, bool> LRigUsherFolder,
    Func<string, string> LRigUsherProgram,
    Func<string, string> LRigUsherPath,
    Func<string, string> LRigUsherName,
    Func<string, string> LRigUsherStem,
    Func<string, string> LRigUsherExtension,
    Func<string, string?, string?> LRigUsherReveal,
    Func<string, string?> LRigUsherExplorer,
    Func<string, string?> LRigUsherLink);
