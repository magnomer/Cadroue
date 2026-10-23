namespace Convention.Tests;

internal sealed record TAuditObjectRow(
    string TAuditObjectName,
    int TAuditObjectParts,
    int TAuditObjectLines,
    int TAuditObjectMembers,
    int TAuditObjectMutable,
    int TAuditObjectOutgoing,
    int TAuditObjectIncoming,
    IReadOnlyList<string> TAuditObjectHubs,
    int TAuditObjectShared,
    int TAuditObjectCrossings,
    double TAuditObjectGlued,
    double TAuditObjectFused,
    double TAuditObjectDensity,
    IReadOnlyList<string> TAuditObjectFlags,
    string TAuditObjectVerdict)
{
    public static TAuditObjectRow TAuditRowCreate(TAuditObjectType type, int outgoing, int incoming)
    {
        List<TAuditObjectMember> members = type.TAuditTypeMembers.Values.ToList();
        int crossings = members.Sum(member => member.TAuditMemberUses.Count(
            target => !ReferenceEquals(member.TAuditMemberPart, target.TAuditMemberPart)));

        HashSet<TAuditObjectMember> hubs = [];
        List<string> hubNames = [];
        foreach (TAuditObjectMember member in members.Where(member => member.TAuditMemberState))
        {
            HashSet<TAuditObjectPart> touching = [member.TAuditMemberPart];
            touching.UnionWith(member.TAuditMemberUsers.Select(user => user.TAuditMemberPart));
            if (touching.Count >= TAuditObjectSetting.TAuditHubLimit["Parts"])
            {
                hubs.Add(member);
                hubNames.Add($"`{member.TAuditMemberSymbol.Name}` reaches {touching.Count} parts");
            }
        }

        int lines = type.TAuditTypeParts.Sum(part => part.TAuditPartLines);
        int parts = type.TAuditTypeParts.Count;
        double glued = TAuditGluedRead(members, parts, []);
        double fused = TAuditGluedRead(members, parts, hubs);
        double density = members.Count == 0 ? 0 : crossings / (double)members.Count;
        TAuditObjectRow row = new(
            type.TAuditTypeSymbol.ToDisplayString(),
            parts,
            lines,
            members.Count,
            members.Count(member => member.TAuditMemberMutable),
            outgoing,
            incoming,
            hubNames,
            hubs.Count,
            crossings,
            glued,
            fused,
            density,
            [],
            string.Empty);
        List<string> flags = TAuditFlagRead(row);
        string verdict = flags.Count > 0 ? flags[0] : parts > 1 ? "Colony" : "Hermit";
        return row with { TAuditObjectFlags = flags, TAuditObjectVerdict = verdict };
    }

    private static List<string> TAuditFlagRead(TAuditObjectRow row)
    {
        bool serpent = row.TAuditObjectLines >= TAuditObjectSetting.TAuditSerpentLimit["Lines"];
        bool centipede = row.TAuditObjectMembers >= TAuditObjectSetting.TAuditCentipedeLimit["Members"];
        bool octopus = row.TAuditObjectOutgoing >= TAuditObjectSetting.TAuditOctopusLimit["Outgoing"];
        bool spider = row.TAuditObjectOutgoing >= TAuditObjectSetting.TAuditSpiderLimit["Outgoing"]
            && row.TAuditObjectIncoming >= TAuditObjectSetting.TAuditSpiderLimit["Incoming"];
        bool hydra = row.TAuditObjectParts >= TAuditObjectSetting.TAuditHydraLimit["Parts"]
            && row.TAuditObjectLines >= TAuditObjectSetting.TAuditHydraLimit["Lines"]
            && (row.TAuditObjectFused >= TAuditObjectSetting.TAuditHydraLimit["Fused"]
                || row.TAuditObjectDensity >= TAuditObjectSetting.TAuditHydraLimit["Density"]);
        List<(string TAuditFlag, bool TAuditHit)> ladder =
        [
            ("Hydra", hydra),
            ("Kraken", (serpent || centipede) && (octopus || spider)),
            ("Spider", spider),
            ("Chameleon", row.TAuditObjectMutable >= TAuditObjectSetting.TAuditChameleonLimit["Mutable"]),
            ("Octopus", octopus),
            ("Centipede", centipede),
            ("Serpent", serpent),
        ];
        return ladder.Where(step => step.TAuditHit).Select(step => step.TAuditFlag).ToList();
    }

    private static double TAuditGluedRead(
        List<TAuditObjectMember> members, int partCount, HashSet<TAuditObjectMember> excluded)
    {
        if (partCount == 0 || members.Count == 0)
        {
            return 0;
        }

        int[] parent = Enumerable.Range(0, members.Count).ToArray();
        int TAuditRootFind(int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }

            return index;
        }

        foreach (TAuditObjectMember member in members.Where(member => !excluded.Contains(member)))
        {
            foreach (TAuditObjectMember target in member.TAuditMemberUses.Where(target => !excluded.Contains(target)))
            {
                parent[TAuditRootFind(member.TAuditMemberIndex)] = TAuditRootFind(target.TAuditMemberIndex);
            }
        }

        int widest = members
            .Where(member => !excluded.Contains(member))
            .GroupBy(member => TAuditRootFind(member.TAuditMemberIndex))
            .Select(group => group.Select(member => member.TAuditMemberPart).Distinct().Count())
            .DefaultIfEmpty(0)
            .Max();
        return widest / (double)partCount;
    }
}
