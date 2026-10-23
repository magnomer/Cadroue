using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditOriginWalker
{
    private static readonly Dictionary<ISymbol, bool> TAuditOriginNames = new(SymbolEqualityComparer.Default);

    private static Compilation? TAuditOriginCompilation;

    internal static string TAuditWriterResolve(ExpressionSyntax? value)
    {
        if (!ReferenceEquals(TAuditOriginCompilation, TAuditBinder.TAuditCompilation))
        {
            TAuditOriginCompilation = TAuditBinder.TAuditCompilation;
            TAuditOriginNames.Clear();
            TAuditOriginSite.TAuditSiteResolve();
        }

        return TAuditWriterResolve(value, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
    }

    private static string TAuditWriterResolve(ExpressionSyntax? value, HashSet<ISymbol> visiting)
    {
        if (value is null)
        {
            return "plain";
        }

        if (TAuditBlankCheck(value))
        {
            return "clear";
        }

        bool asked = value.DescendantNodesAndSelf(node => !TAuditTruthWalker.TAuditNameofCheck(node)
                                                          && !TAuditCapsuleCheck(node)).Any(node =>
            node switch
            {
                MemberAccessExpressionSyntax or MemberBindingExpressionSyntax
                    => TAuditBinderSide.TAuditLogicCheck(TAuditBinderSymbol.TAuditSymbolRead(node)),
                InvocationExpressionSyntax call
                    => TAuditTruthWalker.TAuditCallRead(call) is not null
                       || (TAuditBinderSymbol.TAuditSymbolRead(call) is { } callee
                           && TAuditTruthWalker.TAuditReaderNames.Contains(callee)),
                BaseObjectCreationExpressionSyntax creation => TAuditTruthWalker.TAuditCallRead(creation) is not null,
                IdentifierNameSyntax name => TAuditBinderSymbol.TAuditSymbolRead(name) switch
                {
                    ILocalSymbol or IParameterSymbol when TAuditBinderSide.TAuditLogicCheck(name) => true,
                    { } held => TAuditOriginCheck(held, visiting),
                    _ => false
                },
                _ => false
            });
        return asked ? "engine" : "plain";
    }

    private static bool TAuditOriginCheck(ISymbol held, HashSet<ISymbol> visiting)
    {
        bool top = visiting.Count == 0;
        if (top && TAuditOriginNames.TryGetValue(held, out bool known))
        {
            return known;
        }

        if (!visiting.Add(held))
        {
            return false;
        }

        bool engine = false;
        foreach (ExpressionSyntax? write in TAuditOriginHolder.TAuditOriginRead(held) ?? [])
        {
            if (write is IdentifierNameSyntax or MemberAccessExpressionSyntax
                && TAuditBinderSymbol.TAuditSymbolRead(write) is { } copied && visiting.Contains(copied))
            {
                continue;
            }

            string verdict = TAuditWriterResolve(write, visiting);
            if (verdict == "clear")
            {
                continue;
            }

            engine = verdict == "engine";
            if (!engine)
            {
                break;
            }
        }

        visiting.Remove(held);
        if (top)
        {
            TAuditOriginNames[held] = engine;
        }

        return engine;
    }

    internal static bool TAuditBlankCheck(ExpressionSyntax value)
    {
        if (value.IsKind(SyntaxKind.NullLiteralExpression)
            || value.IsKind(SyntaxKind.DefaultLiteralExpression)
            || value is DefaultExpressionSyntax
            || TAuditBinderSymbol.TAuditSymbolRead(value) is IFieldSymbol
            {
                Name: "Empty", ContainingType.SpecialType: SpecialType.System_String
            })
        {
            return true;
        }

        Optional<object?> constant = TAuditBinder.TAuditModelRead(value).GetConstantValue(value);
        return constant.HasValue && constant.Value is null or "";
    }

    private static bool TAuditCapsuleCheck(SyntaxNode node)
    {
        string? source = node is InvocationExpressionSyntax
                         && TAuditBinderSymbol.TAuditSymbolRead(node) is IMethodSymbol { ContainingType: { } owner }
            ? TAuditBinderSymbol.TAuditSourceRead(owner.OriginalDefinition)
            : null;
        return source is not null && TAuditBinder.TAuditRootRead(TAuditTruthSetting.TAuditCapsuleInclude)
            .Any(folder => source.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase));
    }
}
