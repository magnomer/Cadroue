using Microsoft.CodeAnalysis;

namespace Convention.Tests;

internal static class TAuditBorderDrift
{
    public static IReadOnlyList<TAuditHit> TAuditDriftScan()
    {
        List<(string TAuditDriftName, string? TAuditDriftRing, bool TAuditDriftPublic)> types =
            TAuditBinder.TAuditCompilation.GetSymbolsWithName(_ => true, SymbolFilter.Type)
            .OfType<INamedTypeSymbol>()
            .Select(type => (type.Name,
                TAuditBinderSymbol.TAuditSourceRead(type) is { } source
                    ? TAuditBinder.TAuditRingRead(source, TAuditBorderSetting.TAuditBorderNeighbour.Keys)
                    : null,
                TAuditPublicCheck(type)))
            .ToList();
        Dictionary<string, string[]> offers = TAuditEngineSetting.TAuditEngineOffer
            .Concat(TAuditDeportmentSetting.TAuditDeportmentOffer)
            .Concat(TAuditDemeanorSetting.TAuditDemeanorOffer)
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        return TAuditDriftRead(
            offers, TAuditBorderSetting.TAuditBorderCut, TAuditBorderSetting.TAuditOfferPrefix, types);
    }

    public static IReadOnlyList<TAuditHit> TAuditDriftRead(
        IReadOnlyDictionary<string, string[]> offers,
        IReadOnlyCollection<string> cut,
        IReadOnlyDictionary<string, string[]> prefixes,
        IReadOnlyList<(string TAuditDriftName, string? TAuditDriftRing, bool TAuditDriftPublic)> types)
    {
        Dictionary<string, List<(string TAuditDriftName, string? TAuditDriftRing, bool TAuditDriftPublic)>> typesOf =
            types.GroupBy(type => type.TAuditDriftName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        List<TAuditHit> hits = [];
        foreach ((string pair, string[] offer) in offers)
        {
            string ring = pair[..pair.IndexOf('>')];
            string neighbour = pair[(pair.IndexOf('>') + 1)..];
            string where = "src/" + neighbour;
            string[] allowed = prefixes.GetValueOrDefault(neighbour, []);
            HashSet<string> listed = new(offer, StringComparer.Ordinal);
            foreach (string entry in offer.Distinct(StringComparer.Ordinal))
            {
                List<(string TAuditDriftName, string? TAuditDriftRing, bool TAuditDriftPublic)> found =
                    typesOf.GetValueOrDefault(entry, []);
                string? clause = found.Count == 0 ? "(a) resolves to no type"
                    : found.Count > 1 ? $"(a) resolves to {found.Count} types"
                    : found[0].TAuditDriftRing != neighbour
                        ? $"(a) is declared in {found[0].TAuditDriftRing ?? "no ring"}"
                    : !found[0].TAuditDriftPublic ? "(a) is not public"
                    : !allowed.Any(prefix => entry.StartsWith(prefix, StringComparison.Ordinal))
                        ? $"(a) lacks the prefix {string.Join(", ", allowed)}"
                    : null;
                if (clause is not null)
                {
                    hits.Add(new TAuditHit(where, 0, ring, "Drifting", neighbour, $"{entry} {clause}"));
                }
            }

            if (cut.Contains(ring, StringComparer.Ordinal))
            {
                IEnumerable<string> unlisted = typesOf
                    .Where(item => item.Value.Any(type => type.TAuditDriftRing == neighbour && type.TAuditDriftPublic))
                    .Select(item => item.Key)
                    .Where(name => !listed.Contains(name))
                    .Order(StringComparer.Ordinal);
                foreach (string name in unlisted)
                {
                    hits.Add(new TAuditHit(where, 0, ring, "Drifting", neighbour,
                        $"{name} (b) is public in {neighbour} but not offered"));
                }
            }

            foreach ((string other, string[] otherOffer) in offers
                         .Where(item => item.Key != pair && item.Key[(item.Key.IndexOf('>') + 1)..] == neighbour))
            {
                foreach (string name in otherOffer.Distinct(StringComparer.Ordinal)
                             .Where(name => !listed.Contains(name)))
                {
                    hits.Add(new TAuditHit(where, 0, ring, "Drifting", neighbour,
                        $"{name} (c) is offered by {other} but not here"));
                }
            }
        }

        return hits
            .OrderBy(hit => hit.TAuditHitRing, StringComparer.Ordinal)
            .ThenBy(hit => hit.TAuditHitTarget, StringComparer.Ordinal)
            .ThenBy(hit => hit.TAuditHitName, StringComparer.Ordinal)
            .ToList();
    }

    private static bool TAuditPublicCheck(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    internal static IEnumerable<ISymbol> TAuditPublicRead(INamedTypeSymbol type)
    {
        foreach (ISymbol member in type.GetMembers())
        {
            yield return member;
            if (member is INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public } nested)
            {
                foreach (ISymbol inner in TAuditPublicRead(nested))
                {
                    yield return inner;
                }
            }
        }
    }
}
