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
        }

        veneers = [];
        foreach ((INamedTypeSymbol symbol, List<TypeDeclarationSyntax> type) in parts)
        {
            bool veneer = TAuditVeneerCheck(type);
            if (veneer)
            {
                veneers.Add(symbol.Name);
            }

            foreach (TypeDeclarationSyntax part in type)
            {
                TAuditStorageScan(part, veneer, violations);
                if (veneer)
                {
                    TAuditCallScan(part, violations);
                    TAuditEngineScan(part, violations);
                }
            }
        }

        return violations;
    }

    private static bool TAuditVeneerCheck(IReadOnlyList<TypeDeclarationSyntax> type)
    {
        IReadOnlyList<string> roots = TAuditBinder.TAuditRootRead(TAuditStrictSetting.TAuditVeneerInclude);
        return type.Any(part =>
        {
            string relative = TAuditBinder.TAuditRelativeRead(part.SyntaxTree.FilePath);
            return roots.Any(root => relative.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase));
        });
    }

    private static void TAuditStorageScan(TypeDeclarationSyntax part, bool veneer, List<TViolation> violations)
    {
        string owner = part.Identifier.ValueText;
        foreach (BaseFieldDeclarationSyntax field in part.Members.OfType<BaseFieldDeclarationSyntax>())
        {
            bool fixture = field.Modifiers.Any(modifier =>
                modifier.IsKind(SyntaxKind.ReadOnlyKeyword) || modifier.IsKind(SyntaxKind.ConstKeyword));
            bool shared = field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
            if (!veneer && (fixture || !shared))
            {
                continue;
            }

            string reason = veneer
                ? field is EventFieldDeclarationSyntax ? "event field in a veneer type" : "field in a veneer type"
                : "mutable static field";
            foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
            {
                violations.Add(new TViolation(
                    field.SyntaxTree.FilePath,
                    TAuditLineRead(variable),
                    $"{owner}.{variable.Identifier.ValueText}",
                    "Storage",
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
                "auto-property in a veneer type"));
        }

        foreach (ParameterSyntax parameter in part.ParameterList?.Parameters ?? [])
        {
            violations.Add(new TViolation(
                parameter.SyntaxTree.FilePath,
                TAuditLineRead(parameter),
                $"{owner}.{parameter.Identifier.ValueText}",
                "Storage",
                "primary constructor parameter in a veneer type"));
        }
    }

    private static bool TAuditAutoCheck(PropertyDeclarationSyntax property)
    {
        return property.ExpressionBody is null
               && property.AccessorList is { } accessors
               && accessors.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null);
    }

    private static void TAuditCallScan(TypeDeclarationSyntax part, List<TViolation> violations)
    {
        foreach (MemberDeclarationSyntax member in part.Members)
        {
            List<SyntaxNode> breaches = [];
            foreach (SyntaxNode body in TAuditBodyRead(member))
            {
                TAuditBodyScan(body, breaches);
            }

            HashSet<int> seen = [];
            foreach (SyntaxNode breach in breaches)
            {
                int line = TAuditLineRead(breach);
                if (seen.Add(line))
                {
                    violations.Add(new TViolation(
                        member.SyntaxTree.FilePath,
                        line,
                        $"{part.Identifier.ValueText}.{TAuditMemberRead(member)}",
                        "Call",
                        $"{breach.Kind()} where only a call may stand"));
                }
            }
        }
    }

    private static IEnumerable<SyntaxNode> TAuditBodyRead(MemberDeclarationSyntax member)
    {
        IEnumerable<SyntaxNode?> bodies = member switch
        {
            ConstructorDeclarationSyntax constructor =>
                [constructor.Initializer?.ArgumentList, constructor.Body, constructor.ExpressionBody],
            BaseMethodDeclarationSyntax method => [method.Body, method.ExpressionBody],
            PropertyDeclarationSyntax { ExpressionBody: { } arrow } => [arrow],
            IndexerDeclarationSyntax { ExpressionBody: { } arrow } => [arrow],
            BasePropertyDeclarationSyntax { AccessorList: { } accessors } => accessors.Accessors
                .SelectMany(accessor => new SyntaxNode?[] { accessor.Body, accessor.ExpressionBody }),
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

    private static void TAuditEngineScan(TypeDeclarationSyntax part, List<TViolation> violations)
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
                $"{part.Identifier.ValueText}.{label}",
                "Engine",
                $"reaches the engine through {name.Identifier.ValueText}"));
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
                IdentifierNameSyntax or MemberBindingExpressionSyntax => TAuditBinder.TAuditLogicCheck(child),
                _ => false
            };
            if (logic)
            {
                return true;
            }
        }

        return false;
    }

    private static string TAuditMemberRead(MemberDeclarationSyntax member)
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

    private static int TAuditLineRead(SyntaxNode node)
    {
        return node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
    }
}
