namespace Cadroue.Core;

public sealed record LRigCheckup(
    Func<string, IReadOnlyList<LDossier>?> LRigCheckupReader,
    Func<string, IReadOnlyList<LDossier>, Task> LRigCheckupWriter);
