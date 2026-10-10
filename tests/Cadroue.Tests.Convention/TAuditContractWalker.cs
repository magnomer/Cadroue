using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Convention.Tests;

internal static class TAuditContractWalker
{
    public static IReadOnlyList<TViolation> TAuditRun(
        IReadOnlyList<string> driverPaths, IEnumerable<string> markupPaths)
    {
        List<TViolation> violations = [];
        IReadOnlySet<string> ids = TAuditIdRead(markupPaths);
        IReadOnlyList<SyntaxNode> roots = TAuditBinder.TAuditWalkRead(driverPaths);
        foreach (SyntaxNode root in roots)
        {
            TAuditHardwiringScan(root, violations);
        }

        foreach (SyntaxNode root in roots)
        {
            TAuditLoadScan(root, violations);
            TAuditMasqueradingScan(root, violations);
            TAuditDanglingScan(root, ids, violations);
        }

        return violations;
    }

    private static void TAuditLoadScan(SyntaxNode root, List<TViolation> violations)
    {
        SemanticModel model = TAuditBinder.TAuditModelRead(root);
        foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol { Name: "LoadComponent" } method
                || method.ContainingType?.ToDisplayString() != "System.Windows.Application")
            {
                continue;
            }

            foreach (ArgumentSyntax argument in call.ArgumentList.Arguments)
            {
                if (model.GetTypeInfo(argument.Expression).Type?.ToDisplayString() != "System.Uri"
                    || argument.Expression is BaseObjectCreationExpressionSyntax
                    {
                        ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax { Token.Value: string } }, ..]
                    })
                {
                    continue;
                }

                violations.Add(new TViolation(
                    call.SyntaxTree.FilePath,
                    argument.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    argument.Expression.ToString(),
                    "Hardwiring",
                    "driver loads the surface through a built URI"));
            }
        }
    }

    private static void TAuditHardwiringScan(SyntaxNode root, List<TViolation> violations)
    {
        string path = root.SyntaxTree.FilePath;
        TextLineCollection lines = root.SyntaxTree.GetText().Lines;
        for (int index = 0; index < lines.Count; index++)
        {
            string line = lines[index].ToString();
            string? marker = TAuditStrictSetting.TAuditHardwiringMarkers
                .FirstOrDefault(item => line.Contains(item, StringComparison.Ordinal));
            if (marker is not null)
            {
                violations.Add(new TViolation(path, index + 1, marker, "Hardwiring", "driver line names the surface"));
            }
        }
    }

    private static void TAuditMasqueradingScan(SyntaxNode root, List<TViolation> violations)
    {
        foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (TAuditBinderSymbol.TAuditSymbolRead(type) is not INamedTypeSymbol symbol)
            {
                continue;
            }

            for (INamedTypeSymbol? shape = symbol.BaseType; shape is not null; shape = shape.BaseType)
            {
                string name = shape.OriginalDefinition.ToDisplayString();
                if (TAuditStrictSetting.TAuditMasqueradingTypes.Contains(name, StringComparer.Ordinal))
                {
                    violations.Add(new TViolation(
                        type.SyntaxTree.FilePath,
                        type.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        type.Identifier.ValueText,
                        "Masquerading",
                        $"driver type derives from {name}"));
                    break;
                }
            }
        }
    }

    private static void TAuditDanglingScan(SyntaxNode root, IReadOnlySet<string> ids, List<TViolation> violations)
    {
        SemanticModel model = TAuditBinder.TAuditModelRead(root);
        foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (model.GetSymbolInfo(call).Symbol is not IMethodSymbol method
                || method.ContainingType?.Name != TAuditStrictSetting.TAuditContractType)
            {
                continue;
            }

            TypeDeclarationSyntax? enclosing = call.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (enclosing is not null
                && SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(enclosing), method.ContainingType))
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
                        call.SyntaxTree.FilePath, line, argument.Expression.ToString(), "Dangling",
                        "contract ID is not a constant"));
                }
                else if (!ids.Contains(id))
                {
                    violations.Add(new TViolation(
                        call.SyntaxTree.FilePath, line, id, "Dangling",
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
                document = XDocument.Parse(TAuditBinder.TAuditMarkupRead(path));
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
