using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Convention.Tests;

internal static class TAuditOriginSite
{
    private static Dictionary<string, List<SyntaxNode>> TAuditSiteNames = new(StringComparer.Ordinal);

    internal static void TAuditSiteResolve()
    {
        IReadOnlyList<string> shell = TAuditBinder.TAuditRootRead(
            [.. TAuditTruthSetting.TAuditShellInclude, .. TAuditTruthSetting.TAuditCapsuleInclude]);
        TAuditSiteNames = TAuditBinder.TAuditTrees
            .Where(tree => shell.Any(folder => TAuditBinder.TAuditRelativeRead(tree.FilePath)
                .StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase)))
            .SelectMany(tree => tree.GetRoot().DescendantNodes()
                .Where(node => node is IdentifierNameSyntax or ImplicitObjectCreationExpressionSyntax
                or ConstructorInitializerSyntax))
            .GroupBy(node => node is IdentifierNameSyntax name ? name.Identifier.ValueText : "new",
                StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
    }

    internal static IEnumerable<IdentifierNameSyntax> TAuditSiteRead(ISymbol held)
    {
        return (TAuditSiteNames.GetValueOrDefault(held.Name) ?? [])
            .OfType<IdentifierNameSyntax>()
            .Where(identifier => !identifier.Ancestors().Any(TAuditTruthWalker.TAuditNameofCheck)
                                 && SymbolEqualityComparer.Default.Equals(
                                     TAuditBinderSymbol.TAuditSymbolRead(identifier), held));
    }

    internal static List<ExpressionSyntax?> TAuditPropertyRead(ISymbol held, SyntaxNode declaration)
    {
        List<ExpressionSyntax?> writes = [];
        foreach (BaseObjectCreationExpressionSyntax creation in declaration.DescendantNodes()
                     .OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (TAuditBinder.TAuditModelRead(creation).GetOperation(creation) is IObjectCreationOperation built
                && built.Arguments.FirstOrDefault(argument => argument.Parameter?.Name == "defaultValue")
                    ?.Value.Syntax is ExpressionSyntax fallback
                && !TAuditNeutralCheck(fallback))
            {
                writes.Add(fallback);
            }
        }

        foreach (IdentifierNameSyntax identifier in TAuditSiteRead(held))
        {
            if (identifier.Parent is not ArgumentSyntax
                {
                    Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call } list
                } argument)
            {
                continue;
            }

            switch ((TAuditBinderSymbol.TAuditSymbolRead(call) as IMethodSymbol)?.Name)
            {
                case "SetValue" or "SetCurrentValue" when list.Arguments.Count == 2 && list.Arguments[0] == argument:
                    writes.Add(list.Arguments[1].Expression);
                    break;
                case "GetValue" or "ReadLocalValue" or "ClearValue" or "GetValueSource" or "InvalidateProperty"
                    or "CoerceValue":
                    break;
                default:
                    writes.Add(null);
                    break;
            }
        }

        return writes;
    }

    internal static List<ExpressionSyntax?> TAuditArgumentRead(IMethodSymbol? method, string name)
    {
        List<ExpressionSyntax?> writes = [];
        List<SyntaxNode?> sites;
        if (method is { MethodKind: MethodKind.Ordinary })
        {
            sites = (TAuditSiteNames.GetValueOrDefault(method.Name) ?? [])
                .OfType<IdentifierNameSyntax>()
                .Where(identifier => !identifier.Ancestors().Any(TAuditTruthWalker.TAuditNameofCheck)
                                     && TAuditBinderSymbol.TAuditSymbolRead(identifier) is IMethodSymbol called
                                     && SymbolEqualityComparer.Default.Equals(
                                         (called.ReducedFrom ?? called).OriginalDefinition, method))
                .Select(identifier => identifier.Parent is MemberBindingExpressionSyntax binding
                    ? binding
                    : identifier.Parent is MemberAccessExpressionSyntax access && access.Name == identifier
                        ? access
                        : (ExpressionSyntax)identifier)
                .Select(callee => callee.Parent is InvocationExpressionSyntax call && call.Expression == callee
                    ? call
                    : null)
                .ToList<SyntaxNode?>();
            if (sites.Count == 0 || sites.Contains(null) || method.IsOverride || method.IsVirtual
                || method.IsAbstract || method.IsGenericMethod
                || method.ContainingType.AllInterfaces.SelectMany(face => face.GetMembers()).Any(member =>
                    SymbolEqualityComparer.Default.Equals(
                        method.ContainingType.FindImplementationForInterfaceMember(member), method)))
            {
                return [null];
            }
        }
        else
        {
            sites = (TAuditSiteNames.GetValueOrDefault(method?.ContainingType.Name ?? "") ?? [])
                .Select(node => node.Parent is QualifiedNameSyntax qualified ? qualified.Parent : node.Parent)
                .Concat(TAuditSiteNames.GetValueOrDefault("new") ?? [])
                .Where(site => site is not null
                    && SymbolEqualityComparer.Default.Equals(TAuditBinderSymbol.TAuditSymbolRead(site), method))
                .ToList();
        }

        foreach (SyntaxNode site in sites.OfType<SyntaxNode>())
        {
            ImmutableArray<IArgumentOperation>? built = TAuditBinder.TAuditModelRead(site).GetOperation(site) switch
            {
                IObjectCreationOperation creation => creation.Arguments,
                IInvocationOperation chained => chained.Arguments,
                _ => null
            };
            if (built is null)
            {
                writes.Add(null);
                continue;
            }

            foreach (IArgumentOperation argument in built.Value.Where(argument => argument.Parameter?.Name == name))
            {
                ExpressionSyntax? given = argument.ArgumentKind == ArgumentKind.DefaultValue
                    ? (argument.Parameter?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as ParameterSyntax)
                    ?.Default?.Value
                    : argument.Value.Syntax as ExpressionSyntax;
                if (given is null || !TAuditNeutralCheck(given))
                {
                    writes.Add(given);
                }
            }
        }

        return writes;
    }

    private static bool TAuditNeutralCheck(ExpressionSyntax value)
    {
        if (TAuditOriginWalker.TAuditBlankCheck(value))
        {
            return true;
        }

        Optional<object?> constant = TAuditBinder.TAuditModelRead(value).GetConstantValue(value);
        return constant.HasValue && constant.Value switch
        {
            false => true,
            bool or string or char => false,
            IConvertible number => number.ToDouble(System.Globalization.CultureInfo.InvariantCulture) == 0,
            _ => false
        };
    }
}
