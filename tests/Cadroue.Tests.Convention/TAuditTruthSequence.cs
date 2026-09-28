using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static partial class TAuditTruthWalker
{
    private static HashSet<ISymbol> TAuditSendNames = new(SymbolEqualityComparer.Default);

    private static void TAuditSendResolve(IReadOnlyList<TypeDeclarationSyntax> parts)
    {
        TAuditSendNames = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (MemberDeclarationSyntax member in TAuditScopeRead(parts))
        {
            if (member is not (MethodDeclarationSyntax or PropertyDeclarationSyntax)
                || TAuditBinder.TAuditSymbolRead(member) is not { } symbol)
            {
                continue;
            }

            if (TAuditSendRead(member, true).Count > 0)
            {
                TAuditSendNames.Add(symbol);
            }
        }
    }

    private static List<SyntaxNode> TAuditSendRead(SyntaxNode scope, bool direct)
    {
        List<SyntaxNode> sends = [];
        foreach (SyntaxNode node in scope.DescendantNodes())
        {
            bool send = node switch
            {
                InvocationExpressionSyntax call => TAuditSendCheck(call, direct),
                ObjectCreationExpressionSyntax creation
                    => TAuditBinder.TAuditTypeRead(creation.Type) is { } built
                       && TAuditBinder.TAuditLogicCheck(built)
                       && built.Name.StartsWith(TAuditTruthSetting.TAuditRequestPrefix, StringComparison.Ordinal),
                _ => false
            };
            if (send
                && node.Ancestors().OfType<InvocationExpressionSyntax>().Any(call => TAuditSendCheck(call, direct)))
            {
                continue;
            }

            if (send)
            {
                sends.Add(node);
            }
        }

        return sends;
    }

    private static bool TAuditSendCheck(InvocationExpressionSyntax call, bool direct)
    {
        if (TAuditBinder.TAuditSymbolRead(call) is not { } callee)
        {
            return false;
        }

        return (TAuditBinder.TAuditLogicCheck(callee)
                && TAuditTruthSetting.TAuditSendRoots.Contains(callee.Name, StringComparer.Ordinal))
               || TAuditGateCheck(callee)
               || (!direct && TAuditSendNames.Contains(callee));
    }

    private static bool TAuditGateCheck(ISymbol callee)
    {
        return callee is IMethodSymbol { MethodKind: MethodKind.Ordinary, IsStatic: false } method
               && TAuditBinder.TAuditConductCheck(method.ContainingType);
    }

    private static void TAuditSequenceScan(IReadOnlyList<TypeDeclarationSyntax> type, List<TViolation> violations)
    {
        foreach (MemberDeclarationSyntax scope in TAuditScopeRead(type))
        {
            List<SyntaxNode> sends = TAuditSendRead(scope, false);
            for (int later = 1; later < sends.Count; later++)
            {
                for (int earlier = 0; earlier < later; earlier++)
                {
                    if (TAuditExclusiveCheck(sends[earlier], sends[later]))
                    {
                        continue;
                    }

                    violations.Add(new TViolation(
                        scope.SyntaxTree.FilePath,
                        TAuditLineRead(sends[later]),
                        TAuditMemberRead(scope),
                        "Shape",
                        $"sends a second request after line {TAuditLineRead(sends[earlier])}"));
                    later = sends.Count;
                    break;
                }
            }
        }
    }

    private static bool TAuditExclusiveCheck(SyntaxNode earlier, SyntaxNode later)
    {
        HashSet<SyntaxNode> above = new(later.Ancestors());
        SyntaxNode? shared = earlier.Ancestors().FirstOrDefault(above.Contains);
        if (shared is null)
        {
            return false;
        }

        SyntaxNode armEarlier = TAuditArmRead(earlier, shared);
        SyntaxNode armLater = TAuditArmRead(later, shared);
        return shared switch
        {
            IfStatementSyntax branch => branch.Else is not null && armEarlier != armLater,
            ConditionalExpressionSyntax => armEarlier != armLater,
            SwitchStatementSyntax => armEarlier != armLater,
            SwitchExpressionSyntax => armEarlier != armLater,
            _ => armEarlier is IfStatementSyntax jump && TAuditJumpCheck(jump)
        };
    }

    private static SyntaxNode TAuditArmRead(SyntaxNode node, SyntaxNode shared)
    {
        SyntaxNode arm = node;
        while (arm.Parent is not null && arm.Parent != shared)
        {
            arm = arm.Parent;
        }

        return arm;
    }

    private static bool TAuditJumpCheck(IfStatementSyntax branch)
    {
        return branch.Else is null && branch.Statement.DescendantNodesAndSelf().Any(node =>
            node is ReturnStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax);
    }
}
