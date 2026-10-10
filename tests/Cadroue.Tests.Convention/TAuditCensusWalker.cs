using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditCensusWalker
{
    public static IReadOnlyList<TAuditHit> TAuditDeclaredRead()
    {
        List<TAuditHit> declared = [];
        foreach (SyntaxTree tree in TAuditBinder.TAuditTrees)
        {
            string relative = TAuditBinder.TAuditRelativeRead(tree.FilePath);
            string? ring = TAuditBinder.TAuditRingRead(relative, TAuditBorderSetting.TAuditBorderNeighbour.Keys);
            if (ring is null)
            {
                continue;
            }

            IEnumerable<BaseTypeDeclarationSyntax> declarations = tree.GetRoot()
                .DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>();
            foreach (BaseTypeDeclarationSyntax declaration in declarations)
            {
                int line = declaration.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                declared.Add(new TAuditHit(relative, line, ring, "declared", ring, declaration.Identifier.Text));
            }
        }

        return declared;
    }

    public static IReadOnlyDictionary<string, int> TAuditFileRead()
    {
        return TAuditBinder.TAuditTrees
            .Select(tree => TAuditBinder.TAuditRingRead(
                TAuditBinder.TAuditRelativeRead(tree.FilePath), TAuditBorderSetting.TAuditBorderNeighbour.Keys))
            .Where(ring => ring is not null)
            .GroupBy(ring => ring!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
    }
}
