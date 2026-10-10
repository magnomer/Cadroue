using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditStrictWalker
{
    public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths, out List<string> veneers)
    {
        List<TViolation> violations = [];
        Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> parts = new(SymbolEqualityComparer.Default);
        foreach (SyntaxNode root in TAuditBinder.TAuditWalkRead(sourcePaths))
        {
            foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (TAuditBinderSymbol.TAuditSymbolRead(type) is not INamedTypeSymbol key)
                {
                    continue;
                }

                if (!parts.TryGetValue(key, out List<TypeDeclarationSyntax>? list))
                {
                    list = [];
                    parts[key] = list;
                }

                list.Add(type);
            }

            if (TAuditVeneerCheck(root))
            {
                TAuditHomoglyphScan(root, violations);
                TAuditPlainScan(root, violations);
            }
        }

        veneers = [];
        foreach ((INamedTypeSymbol symbol, List<TypeDeclarationSyntax> type) in parts)
        {
            bool veneer = type.Any(TAuditVeneerCheck);
            if (veneer)
            {
                veneers.Add(symbol.Name);
            }

            foreach (TypeDeclarationSyntax part in type)
            {
                TAuditHoardingScan(part, veneer, violations);
                if (veneer)
                {
                    TAuditStrictMeddling.TAuditMeddlingScan(part.Identifier.ValueText, part.Members, violations);
                    TAuditEngineScan(part, part.Identifier.ValueText, violations);
                    TAuditFreelancingScan(part, violations);
                }
            }
        }

        return violations;
    }

    private static bool TAuditVeneerCheck(SyntaxNode part)
    {
        IReadOnlyList<string> roots = TAuditBinder.TAuditRootRead(TAuditStrictSetting.TAuditVeneerInclude);
        string relative = TAuditBinder.TAuditRelativeRead(part.SyntaxTree.FilePath);
        return roots.Any(root => relative.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static void TAuditPlainScan(SyntaxNode root, List<TViolation> violations)
    {
        foreach (EnumDeclarationSyntax listed in root.DescendantNodes().OfType<EnumDeclarationSyntax>())
        {
            string owner = listed.Identifier.ValueText;
            TAuditEngineScan(listed, owner, violations);
            foreach (EnumMemberDeclarationSyntax member in listed.Members)
            {
                if (member.EqualsValue is { Value: var value } && value is not LiteralExpressionSyntax)
                {
                    violations.Add(new TViolation(
                        member.SyntaxTree.FilePath,
                        TAuditLineRead(member),
                        $"{owner}.{member.Identifier.ValueText}",
                        "Meddling",
                        $"{value.Kind()} where only a call may stand"));
                }
            }
        }

        foreach (DelegateDeclarationSyntax shape in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
        {
            TAuditEngineScan(shape, shape.Identifier.ValueText, violations);
        }
    }

    private static void TAuditHoardingScan(TypeDeclarationSyntax part, bool veneer, List<TViolation> violations)
    {
        string owner = part.Identifier.ValueText;
        foreach (BaseFieldDeclarationSyntax field in part.Members.OfType<BaseFieldDeclarationSyntax>())
        {
            bool constant = field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ConstKeyword));
            bool fixture = constant || field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.ReadOnlyKeyword));
            bool shared = field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
            if (veneer ? constant : fixture || !shared)
            {
                continue;
            }

            string reason = veneer
                ? field is EventFieldDeclarationSyntax ? "event field in a surface type" : "field in a surface type"
                : "mutable static field in a driver type";
            foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
            {
                violations.Add(new TViolation(
                    field.SyntaxTree.FilePath,
                    TAuditLineRead(variable),
                    $"{owner}.{variable.Identifier.ValueText}",
                    veneer ? "Hoarding" : "Sharing",
                    reason));
            }
        }

        if (!veneer)
        {
            return;
        }

        foreach (PropertyDeclarationSyntax property in part.Members.OfType<PropertyDeclarationSyntax>()
                     .Where(TAuditAutoCheck))
        {
            violations.Add(new TViolation(
                property.SyntaxTree.FilePath,
                TAuditLineRead(property),
                $"{owner}.{property.Identifier.ValueText}",
                "Hoarding",
                "auto-property in a surface type"));
        }

        foreach (ParameterSyntax parameter in part.ParameterList?.Parameters ?? [])
        {
            violations.Add(new TViolation(
                parameter.SyntaxTree.FilePath,
                TAuditLineRead(parameter),
                $"{owner}.{parameter.Identifier.ValueText}",
                "Hoarding",
                "primary constructor parameter in a surface type"));
        }
    }

    private static void TAuditFreelancingScan(TypeDeclarationSyntax part, List<TViolation> violations)
    {
        string owner = part.Identifier.ValueText;
        foreach (MemberDeclarationSyntax member in part.Members.Where(member =>
                     member is BaseMethodDeclarationSyntax and not ConstructorDeclarationSyntax
                         or BasePropertyDeclarationSyntax))
        {
            violations.Add(new TViolation(
                member.SyntaxTree.FilePath,
                TAuditLineRead(member),
                $"{owner}.{TAuditMemberRead(member)}",
                "Freelancing",
                $"{member.Kind()} where only a constructor may stand"));
        }
    }

    private static bool TAuditAutoCheck(PropertyDeclarationSyntax property)
    {
        return property.ExpressionBody is null
               && property.AccessorList is { } accessors
               && accessors.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null);
    }

    private static void TAuditEngineScan(SyntaxNode part, string owner, List<TViolation> violations)
    {
        HashSet<int> seen = [];
        IEnumerable<SimpleNameSyntax> names = part
            .DescendantNodes(node => node == part || node is not BaseTypeDeclarationSyntax)
            .OfType<SimpleNameSyntax>();
        foreach (SimpleNameSyntax name in names)
        {
            if (!TAuditBinderSide.TAuditLogicCheck(name))
            {
                continue;
            }

            int line = TAuditLineRead(name);
            if (!seen.Add(line))
            {
                continue;
            }

            MemberDeclarationSyntax? member = name.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            string label = member is null || member == part ? "type" : TAuditMemberRead(member);
            violations.Add(new TViolation(
                part.SyntaxTree.FilePath,
                line,
                $"{owner}.{label}",
                "Prying",
                $"names {name.Identifier.ValueText} from below the driver"));
        }
    }

    public static void TAuditHomoglyphScan(SyntaxNode root, List<TViolation> violations)
    {
        foreach (SyntaxToken token in root.DescendantTokens())
        {
            if (!token.IsKind(SyntaxKind.IdentifierToken) || token.ValueText.All(char.IsAscii))
            {
                continue;
            }

            violations.Add(new TViolation(
                root.SyntaxTree.FilePath,
                TAuditLineRead(token.Parent ?? root),
                token.ValueText,
                "Homoglyph",
                "identifier carries a non-ASCII glyph"));
        }
    }

    public static string TAuditMemberRead(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax => "ctor",
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            EventDeclarationSyntax evt => evt.Identifier.ValueText,
            BaseFieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            OperatorDeclarationSyntax op => op.OperatorToken.ValueText,
            _ => member.Kind().ToString()
        };
    }

    public static int TAuditLineRead(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }
}
