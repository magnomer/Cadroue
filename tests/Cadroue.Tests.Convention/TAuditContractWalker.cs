using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditContractWalker
{
    public static IReadOnlyList<TViolation> TAuditRun(
        IReadOnlyList<string> driverPaths, IEnumerable<string> markupPaths)
    {
        List<TViolation> violations = [];
        IReadOnlySet<string> ids = TAuditIdRead(markupPaths);
        foreach (string path in driverPaths)
        {
            TAuditPackScan(path, violations);
        }

        foreach (SyntaxNode root in TAuditBinder.TAuditWalkRead(driverPaths))
        {
            TAuditScaffoldScan(root, violations);
            TAuditContractScan(root, ids, violations);
        }

        return violations;
    }

    private static void TAuditPackScan(string path, List<TViolation> violations)
    {
        string[] lines = File.ReadAllLines(path);
        for (int index = 0; index < lines.Length; index++)
        {
            string? marker = TAuditStrictSetting.TAuditPackMarkers
                .FirstOrDefault(item => lines[index].Contains(item, StringComparison.Ordinal));
            if (marker is not null)
            {
                violations.Add(new TViolation(path, index + 1, marker, "Pack", "driver line names the surface"));
            }
        }
    }

    private static void TAuditScaffoldScan(SyntaxNode root, List<TViolation> violations)
    {
        foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (TAuditBinder.TAuditSymbolRead(type) is not INamedTypeSymbol symbol)
            {
                continue;
            }

            for (INamedTypeSymbol? shape = symbol.BaseType; shape is not null; shape = shape.BaseType)
            {
                string name = shape.OriginalDefinition.ToDisplayString();
                if (TAuditStrictSetting.TAuditScaffoldTypes.Contains(name, StringComparer.Ordinal))
                {
                    violations.Add(new TViolation(
                        type.SyntaxTree.FilePath,
                        type.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        type.Identifier.ValueText,
                        "Scaffold",
                        $"driver type derives from {name}"));
                    break;
                }
            }
        }
    }

    private static void TAuditContractScan(SyntaxNode root, IReadOnlySet<string> ids, List<TViolation> violations)
    {
        SemanticModel model = TAuditBinder.TAuditModelRead(root);
        foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method
                || method.ContainingType?.Name != TAuditStrictSetting.TAuditContractType)
            {
                continue;
            }

            foreach (ArgumentSyntax argument in call.ArgumentList.Arguments)
            {
                if (model.GetTypeInfo(argument.Expression).ConvertedType?.SpecialType != SpecialType.System_String)
                {
                    continue;
                }

                int line = argument.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                Optional<object?> constant = model.GetConstantValue(argument.Expression);
                if (constant is not { HasValue: true, Value: string id })
                {
                    violations.Add(new TViolation(
                        call.SyntaxTree.FilePath, line, argument.Expression.ToString(), "Contract",
                        "contract ID is not a constant"));
                }
                else if (!ids.Contains(id))
                {
                    violations.Add(new TViolation(
                        call.SyntaxTree.FilePath, line, id, "Contract",
                        "contract ID has no element or resource in the surface"));
                }
            }
        }
    }

    private static IReadOnlySet<string> TAuditIdRead(IEnumerable<string> markupPaths)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (string path in markupPaths)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(path);
            }
            catch (XmlException)
            {
                continue;
            }

            ids.UnionWith(document.Descendants()
                .SelectMany(element => element.Attributes())
                .Where(attribute => !attribute.IsNamespaceDeclaration
                    && TAuditStrictSetting.TAuditContractIds.Contains(attribute.Name.LocalName, StringComparer.Ordinal))
                .Select(attribute => attribute.Value));
        }

        return ids;
    }
}
