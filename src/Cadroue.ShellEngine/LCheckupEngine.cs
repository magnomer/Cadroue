using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LCheckupEngine
{
    public static Func<string, IReadOnlyList<LDossier>?> LCheckupReadSeam { get; set; } = _ => null;

    public static Func<string, IReadOnlyList<LDossier>, Task> LCheckupSaveSeam { get; set; } =
        (_, _) => Task.CompletedTask;
}
