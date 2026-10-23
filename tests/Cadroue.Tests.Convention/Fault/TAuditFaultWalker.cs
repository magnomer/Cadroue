using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Convention.Tests;

internal static class TAuditFaultWalker
{
    public const string TAuditFaultKind = "Swallowing";

    private const string TAuditCancelName = "System.OperationCanceledException";

    public static IReadOnlyList<TViolation> TAuditFaultScan()
    {
        List<SemanticModel> models = TAuditBinder.TAuditTrees
            .Where(tree => TAuditBinder.TAuditRingRead(
                TAuditBinder.TAuditRelativeRead(tree.FilePath), TAuditFaultSetting.TAuditFaultRing) is not null)
            .Select(tree => TAuditBinder.TAuditModelRead(tree))
            .ToList();
        IReadOnlySet<ISymbol> carried = TAuditCarriedRead(models);
        return models
            .SelectMany(model => model.SyntaxTree.GetRoot().DescendantNodes().OfType<CatchClauseSyntax>()
                .Select(clause => TAuditFaultRead(model, clause, carried)))
            .OfType<TViolation>()
            .OrderBy(hit => hit.TViolationPath, StringComparer.Ordinal)
            .ThenBy(hit => hit.TViolationLine)
            .ToList();
    }

    public static TViolation? TAuditFaultRead(
        SemanticModel model, CatchClauseSyntax clause, IReadOnlySet<ISymbol> carried)
    {
        ITypeSymbol? caught = clause.Declaration is { } declaration ? model.GetTypeInfo(declaration.Type).Type : null;
        for (ITypeSymbol? kind = caught; kind is not null; kind = kind.BaseType)
        {
            if (string.Equals(kind.ToDisplayString(), TAuditCancelName, StringComparison.Ordinal))
            {
                return null;
            }
        }

        foreach (SyntaxNode node in TAuditNodeRead(clause.Block))
        {
            if (node is ThrowStatementSyntax or ThrowExpressionSyntax
                || node is InvocationExpressionSyntax invocation
                && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { MethodKind: MethodKind.DelegateInvoke })
            {
                return null;
            }
        }

        if (clause.Declaration is { } named
            && model.GetDeclaredSymbol(named) is ILocalSymbol variable
            && TAuditCarryCheck(model, clause.Block, variable, carried))
        {
            return null;
        }

        ISymbol? owner = model.GetEnclosingSymbol(clause.SpanStart);
        while (owner is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction })
        {
            owner = owner.ContainingSymbol;
        }

        string method = owner switch
        {
            IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } built =>
                built.ContainingType.Name,
            IMethodSymbol { AssociatedSymbol: { } associated } => associated.Name,
            { } symbol => symbol.Name,
            null => "?",
        };
        string type = owner?.ContainingType?.Name ?? "?";
        string shown = clause.Declaration?.Type.ToString() ?? "Exception";
        return new TViolation(
            TAuditBinder.TAuditRelativeRead(model.SyntaxTree.FilePath),
            clause.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            method,
            TAuditFaultKind,
            $"{type}.{method} catch ({shown}) swallows the fault");
    }

    public static bool TAuditCarryCheck(
        SemanticModel model, SyntaxNode body, ISymbol variable, IReadOnlySet<ISymbol> carried)
    {
        foreach (SyntaxNode node in TAuditNodeRead(body))
        {
            switch (node)
            {
                case AssignmentExpressionSyntax assignment when TAuditHoldCheck(model, assignment.Right, variable):
                    if (model.GetSymbolInfo(assignment.Left).Symbol
                        is IFieldSymbol or IPropertySymbol or IParameterSymbol { RefKind: RefKind.Out or RefKind.Ref })
                    {
                        return true;
                    }

                    break;
                case ReturnStatementSyntax { Expression: { } returned } when TAuditHoldCheck(model, returned, variable):
                    return true;
                case InvocationExpressionSyntax invocation
                    when invocation.ArgumentList.Arguments.Any(
                        argument => TAuditHoldCheck(model, argument.Expression, variable)):
                    if (model.GetOperation(invocation) is not IInvocationOperation call)
                    {
                        break;
                    }

                    foreach (IArgumentOperation argument in call.Arguments)
                    {
                        if (argument.Syntax is ArgumentSyntax passed
                            && TAuditHoldCheck(model, passed.Expression, variable)
                            && (call.TargetMethod.MethodKind == MethodKind.DelegateInvoke
                                || argument.Parameter is { } parameter
                                    && carried.Contains(parameter.OriginalDefinition)))
                        {
                            return true;
                        }
                    }

                    break;
            }
        }

        return false;
    }

    private static IReadOnlySet<ISymbol> TAuditCarriedRead(IReadOnlyList<SemanticModel> models)
    {
        List<(SemanticModel TAuditCarriedModel,
            IMethodSymbol TAuditCarriedMethod,
            IMethodSymbol TAuditCarriedKey,
            SyntaxNode TAuditCarriedBody)> bodies = [];
        foreach (SemanticModel model in models)
        {
            foreach (SyntaxNode declaration in model.SyntaxTree.GetRoot().DescendantNodes()
                         .Where(node => node is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax))
            {
                SyntaxNode? body = declaration switch
                {
                    BaseMethodDeclarationSyntax member => (SyntaxNode?)member.Body ?? member.ExpressionBody,
                    LocalFunctionStatementSyntax local => (SyntaxNode?)local.Body ?? local.ExpressionBody,
                    _ => null,
                };
                if (body is not null
                    && model.GetDeclaredSymbol(declaration) is IMethodSymbol { Parameters.Length: > 0 } method)
                {
                    bodies.Add((model, method, method.PartialDefinitionPart ?? method, body));
                }
            }
        }

        HashSet<ISymbol> carried = new(SymbolEqualityComparer.Default);
        bool grown = true;
        while (grown)
        {
            grown = false;
            foreach ((SemanticModel model, IMethodSymbol method, IMethodSymbol key, SyntaxNode body) in bodies)
            {
                for (int index = 0; index < method.Parameters.Length; index++)
                {
                    IParameterSymbol parameter = method.Parameters[index];
                    IParameterSymbol held = key.Parameters[index].OriginalDefinition;
                    if (carried.Contains(held))
                    {
                        continue;
                    }

                    bool returned = body is ArrowExpressionClauseSyntax arrow
                        && !method.ReturnsVoid
                        && TAuditHoldCheck(model, arrow.Expression, parameter);
                    if (returned || TAuditCarryCheck(model, body, parameter, carried))
                    {
                        carried.Add(held);
                        grown = true;
                    }
                }
            }
        }

        return carried;
    }

    private static bool TAuditHoldCheck(SemanticModel model, ExpressionSyntax? expression, ISymbol variable)
    {
        bool TAuditInnerCheck(ExpressionSyntax? inner) => TAuditHoldCheck(model, inner, variable);

        return expression switch
        {
            null => false,
            IdentifierNameSyntax name
                => string.Equals(name.Identifier.ValueText, variable.Name, StringComparison.Ordinal)
                && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name).Symbol, variable),
            ParenthesizedExpressionSyntax parenthesized => TAuditInnerCheck(parenthesized.Expression),
            PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed =>
                TAuditInnerCheck(suppressed.Operand),
            CastExpressionSyntax cast => TAuditInnerCheck(cast.Expression),
            BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } binary => TAuditInnerCheck(binary.Left),
            ConditionalExpressionSyntax conditional =>
                TAuditInnerCheck(conditional.WhenTrue) || TAuditInnerCheck(conditional.WhenFalse),
            BaseObjectCreationExpressionSyntax creation =>
                (creation.ArgumentList?.Arguments.Any(argument => TAuditInnerCheck(argument.Expression)) ?? false)
                || TAuditInnerCheck(creation.Initializer),
            AnonymousObjectCreationExpressionSyntax anonymous =>
                anonymous.Initializers.Any(member => TAuditInnerCheck(member.Expression)),
            TupleExpressionSyntax tuple => tuple.Arguments.Any(argument => TAuditInnerCheck(argument.Expression)),
            CollectionExpressionSyntax collection => collection.Elements.Any(element => element switch
            {
                ExpressionElementSyntax item => TAuditInnerCheck(item.Expression),
                SpreadElementSyntax spread => TAuditInnerCheck(spread.Expression),
                _ => false,
            }),
            ArrayCreationExpressionSyntax array => TAuditInnerCheck(array.Initializer),
            ImplicitArrayCreationExpressionSyntax array => TAuditInnerCheck(array.Initializer),
            InitializerExpressionSyntax initializer => initializer.Expressions.Any(TAuditInnerCheck),
            AssignmentExpressionSyntax assignment => TAuditInnerCheck(assignment.Right),
            WithExpressionSyntax with => TAuditInnerCheck(with.Expression) || TAuditInnerCheck(with.Initializer),
            _ => false,
        };
    }

    private static IEnumerable<SyntaxNode> TAuditNodeRead(SyntaxNode body)
    {
        return body.DescendantNodes(node =>
            node == body || node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax));
    }
}
