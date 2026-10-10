using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Convention.Tests;

internal static class TAuditOriginHolder
{
    internal static List<ExpressionSyntax?>? TAuditOriginRead(ISymbol held)
    {
        ISymbol owner = held is IParameterSymbol
        {
            ContainingSymbol: IMethodSymbol
            {
                MethodKind: MethodKind.PropertySet, AssociatedSymbol: IPropertySymbol property
            }
        }
            ? property
            : held;
        if (owner is not (IFieldSymbol or IPropertySymbol { IsIndexer: false } or ILocalSymbol
                or IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } }
                or IParameterSymbol
                {
                    RefKind: RefKind.None, ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Ordinary }
                })
            || owner.DeclaringSyntaxReferences.IsEmpty
            || !TAuditBinderSide.TAuditShellCheck(owner.ContainingType))
        {
            return null;
        }

        if (owner is IFieldSymbol or IPropertySymbol
            && owner.DeclaredAccessibility != Accessibility.Private
            && owner.ContainingType.AllInterfaces.Any(face =>
                face.ToDisplayString() == "System.Windows.Markup.IComponentConnector"))
        {
            return [null];
        }

        SyntaxNode declaration = owner.DeclaringSyntaxReferences[0].GetSyntax();
        List<ExpressionSyntax?> writes = [];
        if (ReferenceEquals(owner, held))
        {
            switch (owner)
            {
                case IFieldSymbol { Type.Name: "DependencyProperty" or "DependencyPropertyKey" }:
                    return TAuditOriginSite.TAuditPropertyRead(owner, declaration);
                case IPropertySymbol when declaration is PropertyDeclarationSyntax body
                                          && TAuditGetterRead(body) is { } getter:
                    return getter;
                case IPropertySymbol positional when declaration is ParameterSyntax:
                    writes.AddRange(TAuditOriginSite.TAuditArgumentRead(
                        positional.ContainingType.InstanceConstructors.FirstOrDefault(constructor =>
                            constructor.DeclaringSyntaxReferences.Any(source =>
                                source.GetSyntax() is TypeDeclarationSyntax)),
                        positional.Name));
                    break;
                case IParameterSymbol { ContainingSymbol: IMethodSymbol method } parameter:
                    writes.AddRange(TAuditOriginSite.TAuditArgumentRead(method, parameter.Name));
                    break;
                case ILocalSymbol:
                    writes.AddRange(TAuditLocalRead(declaration));
                    break;
                default:
                    if (declaration is VariableDeclaratorSyntax { Initializer.Value: { } start })
                    {
                        writes.Add(start);
                    }
                    else if (declaration is PropertyDeclarationSyntax { Initializer.Value: { } first })
                    {
                        writes.Add(first);
                    }

                    break;
            }
        }

        foreach (IdentifierNameSyntax identifier in TAuditOriginSite.TAuditSiteRead(owner))
        {
            SyntaxNode reference = identifier.Parent is MemberAccessExpressionSyntax access && access.Name == identifier
                ? access
                : identifier;
            if (TAuditTruthReference.TAuditWriteCheck(reference, out ExpressionSyntax? value)
                && (value is not null || reference.Parent is not MemberAccessExpressionSyntax))
            {
                writes.AddRange(TAuditHelperRead(identifier, reference) ?? [value]);
            }
        }

        return writes;
    }

    internal static List<ExpressionSyntax?>? TAuditHelperRead(IdentifierNameSyntax identifier, SyntaxNode reference)
    {
        if (TAuditBinderSymbol.TAuditSymbolRead(identifier) is IParameterSymbol
            {
                RefKind: RefKind.Ref, ContainingSymbol: IMethodSymbol inner
            } held
            && reference.Parent is AssignmentExpressionSyntax
            && TAuditHelperScan(inner, held) is not null)
        {
            return [];
        }

        if (reference.Parent is not ArgumentSyntax
            {
                Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax call }
            } argument
            || !argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword)
            || TAuditBinder.TAuditModelRead(call).GetOperation(call) is not IInvocationOperation invoked)
        {
            return null;
        }

        IParameterSymbol? target = invoked.Arguments.FirstOrDefault(given => given.Syntax == argument)?.Parameter;
        List<IParameterSymbol>? sources = target is null
            ? null
            : TAuditHelperScan(invoked.TargetMethod.OriginalDefinition, target.OriginalDefinition);
        return sources?.Select(source => (invoked.Arguments.FirstOrDefault(given =>
                    SymbolEqualityComparer.Default.Equals(given.Parameter?.OriginalDefinition, source))
                ?.Syntax as ArgumentSyntax)?.Expression)
            .ToList() ?? [null];
    }

    private static List<IParameterSymbol>? TAuditHelperScan(IMethodSymbol method, IParameterSymbol held)
    {
        List<IParameterSymbol> sources = [];
        foreach (IdentifierNameSyntax use in method.DeclaringSyntaxReferences
                     .SelectMany(source => source.GetSyntax().DescendantNodes().OfType<IdentifierNameSyntax>()))
        {
            if (!SymbolEqualityComparer.Default.Equals(TAuditBinderSymbol.TAuditSymbolRead(use), held)
                || !TAuditTruthReference.TAuditWriteCheck(use, out _))
            {
                continue;
            }

            if (use.Parent is not AssignmentExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.SimpleAssignmentExpression
                } set
                || set.Left != use
                || TAuditBinderSymbol.TAuditSymbolRead(set.Right)
                    is not IParameterSymbol { RefKind: RefKind.None } given
                || !SymbolEqualityComparer.Default.Equals(given.ContainingSymbol, method))
            {
                return null;
            }

            sources.Add(given);
        }

        return sources.Count > 0 ? sources : null;
    }

    private static List<ExpressionSyntax?>? TAuditGetterRead(PropertyDeclarationSyntax property)
    {
        if (property.ExpressionBody is { } body)
        {
            return [body.Expression];
        }

        AccessorDeclarationSyntax? getter = property.AccessorList?.Accessors
            .FirstOrDefault(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
        if (getter?.ExpressionBody is { } arrow)
        {
            return [arrow.Expression];
        }

        return getter?.Body?.DescendantNodes(node => node is not (AnonymousFunctionExpressionSyntax
                or LocalFunctionStatementSyntax))
            .OfType<ReturnStatementSyntax>()
            .Select(exit => exit.Expression)
            .ToList();
    }

    private static List<ExpressionSyntax?> TAuditLocalRead(SyntaxNode declaration)
    {
        if (declaration is VariableDeclaratorSyntax variable)
        {
            return variable.Initializer is { } start ? [start.Value] : [];
        }

        if (declaration is ForEachStatementSyntax loop)
        {
            return [loop.Expression];
        }

        if (declaration is not SingleVariableDesignationSyntax || declaration.Parent is DeclarationExpressionSyntax)
        {
            return [null];
        }

        return
        [
            declaration.Ancestors().Select(node => node switch
            {
                IsPatternExpressionSyntax test => test.Expression,
                SwitchStatementSyntax choice => choice.Expression,
                SwitchExpressionSyntax arms => arms.GoverningExpression,
                ForEachVariableStatementSyntax pair => pair.Expression,
                _ => null
            }).FirstOrDefault(tested => tested is not null)
        ];
    }
}
