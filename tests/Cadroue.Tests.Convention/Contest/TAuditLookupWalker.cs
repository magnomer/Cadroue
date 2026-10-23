using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditLookupWalker
{
    internal static (SyntaxNode?, List<SyntaxNode>)? TAuditLookupRead(
        IReadOnlyList<(SyntaxNode, ExpressionSyntax?)> writes)
    {
        ISymbol? shared = null;
        SyntaxNode? engine = null;
        List<SyntaxNode> plain = [];
        foreach ((SyntaxNode reference, ExpressionSyntax? value) in writes)
        {
            List<ExpressionSyntax> calls = [];
            if (!TAuditCallScan(value, calls, new HashSet<ISymbol>(SymbolEqualityComparer.Default)))
            {
                return null;
            }

            foreach (ExpressionSyntax call in calls)
            {
                shared ??= TAuditBinderSymbol.TAuditSymbolRead(call);
                if (!SymbolEqualityComparer.Default.Equals(shared, TAuditBinderSymbol.TAuditSymbolRead(call)))
                {
                    return null;
                }
            }

            List<string> inputs = calls.Select(TAuditInputResolve).ToList();
            if (inputs.Contains("engine"))
            {
                engine ??= reference;
            }
            else if (inputs.Contains("plain"))
            {
                plain.Add(reference);
            }
        }

        return (engine, plain);
    }

    private static bool TAuditCallScan(ExpressionSyntax? value, List<ExpressionSyntax> calls, HashSet<ISymbol> visiting)
    {
        while (value is ParenthesizedExpressionSyntax or CastExpressionSyntax
               or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
        {
            value = value switch
            {
                ParenthesizedExpressionSyntax group => group.Expression,
                CastExpressionSyntax cast => cast.Expression,
                PostfixUnaryExpressionSyntax forgiven => forgiven.Operand,
                _ => value
            };
        }

        if (value is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax
            && TAuditBinderSymbol.TAuditSymbolRead(value) is IMethodSymbol callee
            && TAuditBinderSide.TAuditShellCheck(callee.ContainingType)
            && TAuditTruthWalker.TAuditCallRead(value) is null)
        {
            calls.Add(value);
            return true;
        }

        return value is IdentifierNameSyntax or MemberAccessExpressionSyntax
               && TAuditBinderSymbol.TAuditSymbolRead(value) is { } held
               && visiting.Add(held)
               && TAuditOriginHolder.TAuditOriginRead(held) is { Count: > 0 } writes
               && writes.Where(write => TAuditOriginWalker.TAuditWriterResolve(write) != "clear")
                   .All(write => TAuditCallScan(write, calls, visiting));
    }

    private static string TAuditInputResolve(ExpressionSyntax call)
    {
        IEnumerable<ExpressionSyntax> inputs = call switch
        {
            InvocationExpressionSyntax invocation => invocation.ArgumentList.Arguments
                .Select(argument => argument.Expression)
                .Concat(invocation.Expression is MemberAccessExpressionSyntax access
                        && TAuditBinderSymbol.TAuditSymbolRead(call) is { IsStatic: false }
                    ? [access.Expression]
                    : []),
            BaseObjectCreationExpressionSyntax creation => (creation.ArgumentList?.Arguments
                    .Select(argument => argument.Expression) ?? [])
                .Concat(creation.Initializer?.Expressions.Select(entry =>
                    entry is AssignmentExpressionSyntax set ? set.Right : entry) ?? []),
            _ => []
        };
        SemanticModel model = TAuditBinder.TAuditModelRead(call);
        List<string> verdicts = inputs
            .Where(input => !model.GetConstantValue(input).HasValue)
            .Select(TAuditOriginWalker.TAuditWriterResolve)
            .ToList();
        return verdicts.Contains("engine") ? "engine" : verdicts.Contains("plain") ? "plain" : "key";
    }
}
