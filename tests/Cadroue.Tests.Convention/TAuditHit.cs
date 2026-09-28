namespace Convention.Tests;

internal sealed record TAuditHit(
    string TAuditHitPath,
    int TAuditHitLine,
    string TAuditHitRing,
    string TAuditHitKind,
    string TAuditHitTarget,
    string TAuditHitName)
{
    public string TAuditPairRead() => $"{TAuditHitKind}:{TAuditHitRing}>{TAuditHitTarget}";

    public string TAuditWaiverRead(IReadOnlyList<string> waivers)
    {
        return waivers.FirstOrDefault(waiver =>
        {
            int split = waiver.LastIndexOf(':');
            string pattern = waiver[..split];
            if (!string.Equals(waiver[(split + 1)..], TAuditHitName, StringComparison.Ordinal))
            {
                return false;
            }

            return pattern.EndsWith("/*", StringComparison.Ordinal)
                ? TAuditHitPath.StartsWith(pattern[..^1], StringComparison.Ordinal)
                : string.Equals(pattern, TAuditHitPath, StringComparison.Ordinal);
        }) ?? "";
    }
}
