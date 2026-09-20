using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditStrictWalker
{
    private static readonly SyntaxKind[] TAuditFlowKinds =
    [
        SyntaxKind.IfStatement,
        SyntaxKind.SwitchStatement,
        SyntaxKind.SwitchExpression,
        SyntaxKind.ConditionalExpression,
        SyntaxKind.ForStatement,
        SyntaxKind.ForEachStatement,
        SyntaxKind.ForEachVariableStatement,
        SyntaxKind.WhileStatement,
        SyntaxKind.DoStatement,
        SyntaxKind.LogicalAndExpression,
        SyntaxKind.LogicalOrExpression,
        SyntaxKind.CoalesceExpression,
        SyntaxKind.AddExpression,
        SyntaxKind.SubtractExpression,
        SyntaxKind.MultiplyExpression,
        SyntaxKind.DivideExpression,
        SyntaxKind.ModuloExpression,
        SyntaxKind.LessThanExpression,
        SyntaxKind.LessThanOrEqualExpression,
        SyntaxKind.GreaterThanExpression,
        SyntaxKind.GreaterThanOrEqualExpression,
        SyntaxKind.EqualsExpression,
        SyntaxKind.NotEqualsExpression,
        SyntaxKind.IsPatternExpression,
        SyntaxKind.LocalFunctionStatement,
    ];

    public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths, out List<string> veneers)
    {
        List<TViolation> violations = [];
        Dictionary<INamedTypeSymbol, List<ClassDeclarationSyntax>> parts = new(SymbolEqualityComparer.Default);
        foreach (SyntaxNode root in TAuditBinder.TAuditWalkRead(sourcePaths).Where(TAuditBinder.TAuditWalkCheck))
        {
            TAuditTreatScan(root, violations);
            TAuditGlyphScan(root, violations);
            foreach (ClassDeclarationSyntax type in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (TAuditBinder.TAuditSymbolRead(type) is not INamedTypeSymbol key)
                {
                    continue;
                }

                if (!parts.TryGetValue(key, out List<ClassDeclarationSyntax>? list))
                {
                    list = [];
                    parts[key] = list;
                }

                list.Add(type);
            }
        }

        veneers = [];
        foreach ((INamedTypeSymbol symbol, List<ClassDeclarationSyntax> type) in parts)
        {
            bool veneer = TAuditVeneerCheck(symbol, type);
            if (veneer)
            {
                veneers.Add(symbol.Name);
            }

            foreach (ClassDeclarationSyntax part in type)
            {
                TAuditStorageScan(part, veneer, violations);
                if (veneer)
                {
                    TAuditFlowScan(part, violations);
                }
            }
        }

        return violations;
    }

    private static bool TAuditVeneerCheck(INamedTypeSymbol symbol, IReadOnlyList<ClassDeclarationSyntax> type)
    {
        if (TAuditBinder.TAuditControlCheck(symbol))
        {
            return true;
        }

        string repoRoot = TAuditSource.TAuditRootRead();
        IReadOnlyList<string> roots = TAuditBinder.TAuditRootRead(TAuditStrictSetting.TAuditVeneerInclude);
        return type.Any(part =>
        {
            string relative = Path.GetRelativePath(repoRoot, part.SyntaxTree.FilePath).Replace('\\', '/');
            return roots.Any(root => relative.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase));
        });
    }

    private static void TAuditStorageScan(ClassDeclarationSyntax part, bool veneer, List<TViolation> violations)
    {
        foreach (FieldDeclarationSyntax field in part.Members.OfType<FieldDeclarationSyntax>())
        {
            bool fixture = field.Modifiers.Any(modifier =>
                modifier.IsKind(SyntaxKind.ReadOnlyKeyword) || modifier.IsKind(SyntaxKind.ConstKeyword));
            bool shared = field.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword));
            if (fixture || (!veneer && !shared))
            {
                continue;
            }

            foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
            {
                if (TAuditWiredCheck(variable))
                {
                    continue;
                }

                violations.Add(new TViolation(
                    field.SyntaxTree.FilePath,
                    TAuditLineRead(variable),
                    $"{part.Identifier.ValueText}.{variable.Identifier.ValueText}",
                    "Storage",
                    shared ? "mutable static field" : "mutable field in a veneer class"));
            }
        }
    }

    private static void TAuditFlowScan(ClassDeclarationSyntax part, List<TViolation> violations)
    {
        foreach (MemberDeclarationSyntax member in part.Members)
        {
            if (member is BaseTypeDeclarationSyntax or FieldDeclarationSyntax)
            {
                continue;
            }

            List<string> found = [];
            foreach (SyntaxNode node in member.DescendantNodes())
            {
                SyntaxKind kind = node.Kind();
                if (!TAuditFlowKinds.Contains(kind))
                {
                    continue;
                }

                string label = kind.ToString();
                if (!found.Contains(label, StringComparer.Ordinal))
                {
                    found.Add(label);
                }
            }

            if (found.Count == 0)
            {
                continue;
            }

            violations.Add(new TViolation(
                member.SyntaxTree.FilePath,
                TAuditLineRead(member),
                $"{part.Identifier.ValueText}.{TAuditMemberRead(member)}",
                "Flow",
                string.Join(' ', found)));
        }
    }

    private static void TAuditTreatScan(SyntaxNode root, List<TViolation> violations)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            string? reason = node switch
            {
                BinaryExpressionSyntax binary
                    when !binary.IsKind(SyntaxKind.CoalesceExpression)
                         && !binary.IsKind(SyntaxKind.LogicalAndExpression)
                         && !binary.IsKind(SyntaxKind.LogicalOrExpression)
                         && !TAuditNullCheck(binary.Left) && !TAuditNullCheck(binary.Right)
                         && (TAuditDataCheck(binary.Left) || TAuditDataCheck(binary.Right))
                    => $"logic value in {binary.OperatorToken.ValueText}",
                InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } query
                    when TAuditStrictSetting.TAuditTreatVerbs.Contains(
                             access.Name.Identifier.ValueText, StringComparer.Ordinal)
                         && (TAuditDataCheck(access.Expression)
                             || query.ArgumentList.Arguments.Any(argument => TAuditDataCheck(argument.Expression)))
                    => $"logic value queried by {access.Name.Identifier.ValueText}",
                CastExpressionSyntax cast when TAuditDataCheck(cast.Expression)
                    => $"logic value cast to {cast.Type}",
                TypeOfExpressionSyntax reflected when TAuditBinder.TAuditLogicCheck(reflected.Type)
                    => "logic type taken by typeof",
                AttributeArgumentSyntax argument when TAuditDataCheck(argument.Expression)
                    => "logic value in an attribute",
                IfStatementSyntax branch when TAuditConditionCheck(branch.Condition)
                    => "logic value decides an if",
                ConditionalExpressionSyntax choice when TAuditConditionCheck(choice.Condition)
                    => "logic value decides a ternary",
                SwitchStatementSyntax select when TAuditDataCheck(select.Expression)
                    => "logic value decides a switch",
                SwitchExpressionSyntax arms when TAuditDataCheck(arms.GoverningExpression)
                    => "logic value decides a switch expression",
                _ => null
            };

            if (reason is null)
            {
                continue;
            }

            int line = TAuditLineRead(node);
            if (seen.Add($"{line}:{reason}"))
            {
                string excerpt = node.ToString().Split('\n')[0].Trim();
                violations.Add(new TViolation(root.SyntaxTree.FilePath, line, excerpt, "Treat", reason));
            }
        }
    }

    private static bool TAuditConditionCheck(ExpressionSyntax condition)
    {
        if (condition is IsPatternExpressionSyntax { Pattern: var pattern } && TAuditPatternCheck(pattern))
        {
            return false;
        }

        if (condition is BinaryExpressionSyntax binary
            && (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression))
            && (TAuditNullCheck(binary.Left) || TAuditNullCheck(binary.Right)))
        {
            return false;
        }

        return !TAuditVerdictCheck(condition) && TAuditDataCheck(condition);
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

    private static bool TAuditVerdictCheck(ExpressionSyntax condition)
    {
        ExpressionSyntax core = TAuditCoreRead(condition);
        return core is InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax
               && TAuditBinder.TAuditLogicCheck(TAuditBinder.TAuditSymbolRead(core));
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

    private static void TAuditGlyphScan(SyntaxNode root, List<TViolation> violations)
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
                "Treat",
                "identifier carries a non-ASCII glyph"));
        }
    }

    private static bool TAuditWiredCheck(VariableDeclaratorSyntax variable)
    {
        return variable.Initializer?.Value is PostfixUnaryExpressionSyntax
        {
            RawKind: (int)SyntaxKind.SuppressNullableWarningExpression,
            Operand: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression }
        };
    }

    private static string TAuditMemberRead(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax => "ctor",
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            EventDeclarationSyntax evt => evt.Identifier.ValueText,
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
