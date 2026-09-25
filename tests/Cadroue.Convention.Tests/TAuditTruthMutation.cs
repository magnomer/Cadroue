using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static partial class TAuditTruthWalker
{
    private static void TAuditMutationScan(SyntaxNode root, List<TViolation> violations)
    {
        foreach (AssignmentExpressionSyntax assignment in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                continue;
            }

            if (assignment.Left is TupleExpressionSyntax tuple
                && tuple.Arguments.Any(argument => argument.Expression is ElementAccessExpressionSyntax))
            {
                ElementAccessExpressionSyntax slot = tuple.Arguments
                    .Select(argument => argument.Expression)
                    .OfType<ElementAccessExpressionSyntax>()
                    .First();
                violations.Add(new TViolation(
                    root.SyntaxTree.FilePath,
                    TAuditLineRead(assignment),
                    slot.Expression.ToString(),
                    "Mutation",
                    "reorders a collection with a swap"));
                continue;
            }

            if (assignment.Parent is InitializerExpressionSyntax { Parent: WithExpressionSyntax }
                && assignment.Left is IdentifierNameSyntax field
                && TAuditBinder.TAuditLogicCheck(TAuditBinder.TAuditSymbolRead(field)))
            {
                violations.Add(new TViolation(
                    root.SyntaxTree.FilePath,
                    TAuditLineRead(assignment),
                    field.Identifier.ValueText,
                    "Mutation",
                    "overrides a logic member in a record copy"));
                continue;
            }

            if (assignment.Left is ElementAccessExpressionSyntax { Expression: var rows }
                && TAuditStoreCheck(rows)
                && !TAuditFreshCheck(rows))
            {
                violations.Add(new TViolation(
                    root.SyntaxTree.FilePath,
                    TAuditLineRead(assignment),
                    rows.ToString(),
                    "Mutation",
                    "overwrites a slot of a row store"));
                continue;
            }

            if (assignment.Parent is InitializerExpressionSyntax
                || assignment.Left is not MemberAccessExpressionSyntax target
                || !TAuditBinder.TAuditLogicCheck(TAuditBinder.TAuditSymbolRead(target)))
            {
                continue;
            }

            violations.Add(new TViolation(
                root.SyntaxTree.FilePath,
                TAuditLineRead(assignment),
                target.ToString(),
                "Mutation",
                "assigns a logic member from the shell"));
        }

        foreach (InvocationExpressionSyntax call in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (call.Expression is not MemberAccessExpressionSyntax access
                || !TAuditTruthSetting.TAuditOrderVerbs.Contains(
                    access.Name.Identifier.ValueText, StringComparer.Ordinal)
                || !TAuditStoreCheck(access.Expression))
            {
                continue;
            }

            violations.Add(new TViolation(
                root.SyntaxTree.FilePath,
                TAuditLineRead(call),
                access.Expression.ToString(),
                "Mutation",
                $"reorders a collection with {access.Name.Identifier.ValueText}"));
        }
    }

    private static bool TAuditFreshCheck(ExpressionSyntax rows)
    {
        if (TAuditBinder.TAuditSymbolRead(rows) is not ILocalSymbol local)
        {
            return false;
        }

        return local.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<VariableDeclaratorSyntax>()
            .Any(declarator => declarator.Initializer?.Value
                is CollectionExpressionSyntax { Elements.Count: 0 }
                or BaseObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0, Initializer: null });
    }

    private static bool TAuditStoreCheck(ExpressionSyntax rows)
    {
        ITypeSymbol? type = TAuditBinder.TAuditTypeRead(rows);
        if (type is null || type.TypeKind == TypeKind.Error)
        {
            return true;
        }

        IEnumerable<ITypeSymbol> held = type switch
        {
            IArrayTypeSymbol array => [array.ElementType],
            INamedTypeSymbol named => named.TypeArguments,
            _ => []
        };
        return held.Any(part => TAuditBinder.TAuditLogicCheck(part)
                                || TAuditBinder.TAuditShellCheck(part)
                                || part.SpecialType == SpecialType.System_Object);
    }
}
