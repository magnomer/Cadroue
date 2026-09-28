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
                if (TAuditBinder.TAuditSymbolRead(type) is not INamedTypeSymbol key)
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
                TAuditGlyphScan(root, violations);
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
                TAuditStorageScan(part, veneer, violations);
                if (veneer)
                {
                    TAuditCallScan(part.Identifier.ValueText, part.Members, violations);
                    TAuditEngineScan(part, part.Identifier.ValueText, violations);
                    TAuditShellScan(part, violations);
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
                        "Call",
                        $"{value.Kind()} where only a call may stand"));
                }
            }
        }

        foreach (DelegateDeclarationSyntax shape in root.DescendantNodes().OfType<DelegateDeclarationSyntax>())
        {
            TAuditEngineScan(shape, shape.Identifier.ValueText, violations);
        }
    }

    private static void TAuditStorageScan(TypeDeclarationSyntax part, bool veneer, List<TViolation> violations)
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
                    veneer ? "Storage" : "Static",
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
                "Storage",
                "auto-property in a surface type"));
        }

        foreach (ParameterSyntax parameter in part.ParameterList?.Parameters ?? [])
        {
            violations.Add(new TViolation(
                parameter.SyntaxTree.FilePath,
                TAuditLineRead(parameter),
                $"{owner}.{parameter.Identifier.ValueText}",
                "Storage",
                "primary constructor parameter in a surface type"));
        }
    }

    private static void TAuditShellScan(TypeDeclarationSyntax part, List<TViolation> violations)
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
                "Shell",
                $"{member.Kind()} where only a constructor may stand"));
        }
    }

    private static bool TAuditAutoCheck(PropertyDeclarationSyntax property)
    {
        return property.ExpressionBody is null
               && property.AccessorList is { } accessors
               && accessors.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null);
    }

    private static void TAuditCallScan(
        string owner, IEnumerable<MemberDeclarationSyntax> members, List<TViolation> violations)
    {
        foreach (MemberDeclarationSyntax member in members)
        {
            List<SyntaxNode> breaches = [];
            foreach (SyntaxNode body in TAuditBodyRead(member))
            {
                TAuditBodyScan(body, breaches);
            }

            HashSet<int> seen = [];
            foreach (SyntaxNode breach in breaches.SelectMany(TAuditNestRead))
            {
                int line = TAuditLineRead(breach);
                if (seen.Add(line))
                {
                    violations.Add(new TViolation(
                        member.SyntaxTree.FilePath,
                        line,
                        $"{owner}.{TAuditMemberRead(member)}",
                        "Call",
                        $"{breach.Kind()} where only a call may stand"));
                }
            }
        }
    }

    private static IEnumerable<SyntaxNode> TAuditNestRead(SyntaxNode breach)
    {
        return breach.DescendantNodesAndSelf().Where(node =>
            node == breach
            || node is StatementSyntax and not BlockSyntax
            || node is SwitchExpressionArmSyntax);
    }

    private static IEnumerable<SyntaxNode> TAuditBodyRead(MemberDeclarationSyntax member)
    {
        IEnumerable<SyntaxNode?> bodies = member switch
        {
            ConstructorDeclarationSyntax constructor =>
                [constructor.Initializer?.ArgumentList, constructor.Body, constructor.ExpressionBody],
            BaseMethodDeclarationSyntax method => [method.Body, method.ExpressionBody],
            BaseFieldDeclarationSyntax field => field.Declaration.Variables
                .Select(variable => (SyntaxNode?)variable.Initializer?.Value),
            PropertyDeclarationSyntax { ExpressionBody: { } arrow } => [arrow],
            IndexerDeclarationSyntax { ExpressionBody: { } arrow } => [arrow],
            BasePropertyDeclarationSyntax { AccessorList: { } accessors } property => accessors.Accessors
                .SelectMany(accessor => new SyntaxNode?[] { accessor.Body, accessor.ExpressionBody })
                .Append((property as PropertyDeclarationSyntax)?.Initializer?.Value),
            _ => []
        };
        return bodies.OfType<SyntaxNode>();
    }

    private static void TAuditBodyScan(SyntaxNode body, List<SyntaxNode> breaches)
    {
        switch (body)
        {
            case BlockSyntax block:
                foreach (StatementSyntax statement in block.Statements)
                {
                    TAuditStatementScan(statement, breaches);
                }

                break;
            case ArrowExpressionClauseSyntax arrow:
                TAuditInvocationScan(arrow.Expression, breaches);
                break;
            case ArgumentListSyntax arguments:
                TAuditArgumentScan(arguments, breaches);
                break;
            case ExpressionSyntax expression:
                TAuditInvocationScan(expression, breaches);
                break;
            default:
                breaches.Add(body);
                break;
        }
    }

    private static void TAuditStatementScan(StatementSyntax statement, List<SyntaxNode> breaches)
    {
        switch (statement)
        {
            case ExpressionStatementSyntax { Expression: var expression }:
                TAuditInvocationScan(expression, breaches);
                break;
            case ReturnStatementSyntax { Expression: { } expression }:
                TAuditInvocationScan(expression, breaches);
                break;
            default:
                breaches.Add(statement);
                break;
        }
    }

    private static void TAuditInvocationScan(ExpressionSyntax expression, List<SyntaxNode> breaches)
    {
        if (expression is not InvocationExpressionSyntax call)
        {
            breaches.Add(expression);
            return;
        }

        switch (call.Expression)
        {
            case SimpleNameSyntax:
                break;
            case MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                TAuditOperandScan(access.Expression, breaches);
                break;
            default:
                breaches.Add(call.Expression);
                break;
        }

        if (TAuditBinder.TAuditSymbolRead(call) is IMethodSymbol method
            && TAuditStrictSetting.TAuditQueryTypes.Contains(
                (method.ReducedFrom ?? method).ContainingType.ToDisplayString(), StringComparer.Ordinal))
        {
            breaches.Add(call);
        }

        TAuditArgumentScan(call.ArgumentList, breaches);
    }

    private static void TAuditArgumentScan(ArgumentListSyntax arguments, List<SyntaxNode> breaches)
    {
        foreach (ArgumentSyntax argument in arguments.Arguments)
        {
            if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
            {
                breaches.Add(argument);
                continue;
            }

            TAuditOperandScan(argument.Expression, breaches);
        }
    }

    private static void TAuditOperandScan(ExpressionSyntax operand, List<SyntaxNode> breaches)
    {
        switch (operand)
        {
            case SimpleNameSyntax or ThisExpressionSyntax or BaseExpressionSyntax or PredefinedTypeSyntax
                or LiteralExpressionSyntax:
                break;
            case MemberAccessExpressionSyntax access when access.IsKind(SyntaxKind.SimpleMemberAccessExpression):
                TAuditOperandScan(access.Expression, breaches);
                break;
            case InvocationExpressionSyntax call:
                TAuditInvocationScan(call, breaches);
                break;
            case AnonymousFunctionExpressionSyntax lambda:
                TAuditBodyScan(lambda.Body, breaches);
                break;
            default:
                breaches.Add(operand);
                break;
        }
    }

    private static void TAuditEngineScan(SyntaxNode part, string owner, List<TViolation> violations)
    {
        HashSet<int> seen = [];
        IEnumerable<SimpleNameSyntax> names = part
            .DescendantNodes(node => node == part || node is not BaseTypeDeclarationSyntax)
            .OfType<SimpleNameSyntax>();
        foreach (SimpleNameSyntax name in names)
        {
            if (!TAuditBinder.TAuditLogicCheck(name))
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
                "Depth",
                $"names {name.Identifier.ValueText} from below the driver"));
        }
    }

    public static void TAuditGlyphScan(SyntaxNode root, List<TViolation> violations)
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
                "Glyph",
                "identifier carries a non-ASCII glyph"));
        }
    }

    public static ExpressionSyntax TAuditCoreRead(ExpressionSyntax condition)
    {
        ExpressionSyntax core = condition;
        while (true)
        {
            core = core switch
            {
                ParenthesizedExpressionSyntax wrapped => wrapped.Expression,
                PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negated
                    => negated.Operand,
                _ => core
            };
            if (core is not (ParenthesizedExpressionSyntax or PrefixUnaryExpressionSyntax))
            {
                return core;
            }
        }
    }

    public static bool TAuditPatternCheck(PatternSyntax pattern)
    {
        return pattern switch
        {
            ConstantPatternSyntax constant => TAuditNullCheck(constant.Expression),
            UnaryPatternSyntax not => TAuditPatternCheck(not.Pattern),
            DeclarationPatternSyntax => true,
            VarPatternSyntax => true,
            TypePatternSyntax => true,
            RecursivePatternSyntax { PositionalPatternClause: null } shape
                => shape.PropertyPatternClause?.Subpatterns.Count is null or 0,
            _ => false
        };
    }

    public static bool TAuditNullCheck(ExpressionSyntax expression)
    {
        return expression.IsKind(SyntaxKind.NullLiteralExpression)
               || expression.IsKind(SyntaxKind.DefaultLiteralExpression);
    }

    public static bool TAuditDataCheck(SyntaxNode node)
    {
        foreach (SyntaxNode child in node.DescendantNodesAndSelf())
        {
            bool logic = child switch
            {
                IdentifierNameSyntax or MemberBindingExpressionSyntax => TAuditBinder.TAuditEngineCheck(child),
                _ => false
            };
            if (logic)
            {
                return true;
            }
        }

        return false;
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
