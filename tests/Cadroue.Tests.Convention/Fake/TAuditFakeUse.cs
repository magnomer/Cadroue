using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditFakeUse
{
    private const string TAuditRootReader = "";

    public static void TAuditUseScan(
        SemanticModel model,
        Dictionary<string, TAuditFakeMember> members,
        HashSet<string> names,
        bool test)
    {
        SyntaxNode root = model.SyntaxTree.GetRoot();
        foreach (SimpleNameSyntax name in root.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            if (!names.Contains(name.Identifier.ValueText))
            {
                continue;
            }

            SymbolInfo info = model.GetSymbolInfo(name);
            IEnumerable<ISymbol> bound = info.Symbol is ISymbol symbol ? [symbol] : info.CandidateSymbols;
            foreach (ISymbol target in bound)
            {
                TAuditUseAdd(model, name, target, members, test);
            }
        }

        foreach (SyntaxNode creation in root.DescendantNodes().Where(node =>
                     node is BaseObjectCreationExpressionSyntax or AttributeSyntax))
        {
            if (model.GetSymbolInfo(creation).Symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } built)
            {
                TAuditUseAdd(model, creation, built.ContainingType, members, test);
            }
        }

        foreach (CommonForEachStatementSyntax loop in root.DescendantNodes().OfType<CommonForEachStatementSyntax>())
        {
            ForEachStatementInfo info = model.GetForEachStatementInfo(loop);
            ISymbol?[] targets = [info.GetEnumeratorMethod, info.MoveNextMethod, info.CurrentProperty];
            foreach (ISymbol target in targets.OfType<ISymbol>())
            {
                TAuditUseAdd(model, loop, target, members, test);
            }
        }
    }

    private static void TAuditUseAdd(
        SemanticModel model,
        SyntaxNode site,
        ISymbol target,
        Dictionary<string, TAuditFakeMember> members,
        bool test)
    {
        ISymbol normal = TAuditFakeSymbol.TAuditNormalRead(target);
        List<TAuditFakeMember> reached = new[] { normal }
            .Concat(TAuditFakeSymbol.TAuditContractRead(normal))
            .Select(symbol => members.GetValueOrDefault(TAuditFakeSymbol.TAuditKeyRead(symbol)))
            .OfType<TAuditFakeMember>()
            .ToList();
        if (reached.Count == 0)
        {
            return;
        }

        string? owner = TAuditOwnerRead(model, site, members);
        foreach (TAuditFakeMember member in reached)
        {
            if (owner == member.TAuditMemberKey || !TAuditReadCheck(site, normal, member))
            {
                continue;
            }

            if (test)
            {
                member.TAuditMemberTesters.Add(TAuditLabelRead(model, site));
            }
            else
            {
                member.TAuditMemberReaders.Add(owner ?? TAuditRootReader);
            }
        }
    }

    private static bool TAuditReadCheck(SyntaxNode site, ISymbol target, TAuditFakeMember member)
    {
        if (site is not ExpressionSyntax expression || target is IMethodSymbol)
        {
            return true;
        }

        while (expression.Parent is MemberAccessExpressionSyntax access && access.Name == expression)
        {
            expression = access;
        }

        if (expression.Parent is MemberBindingExpressionSyntax binding && binding.Name == expression)
        {
            expression = binding;
        }

        SyntaxNode? parent = expression.Parent;
        if (target is IEventSymbol)
        {
            return !member.TAuditMemberStored
                || parent is AssignmentExpressionSyntax
                {
                    RawKind: (int)SyntaxKind.AddAssignmentExpression or (int)SyntaxKind.SubtractAssignmentExpression,
                } subscription && subscription.Left == expression;
        }

        if (!member.TAuditMemberStored)
        {
            return true;
        }

        return parent switch
        {
            AssignmentExpressionSyntax assignment when assignment.Left == expression =>
                assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression),
            PostfixUnaryExpressionSyntax or PrefixUnaryExpressionSyntax => parent.Kind() is not (
                SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression
                or SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression),
            ArgumentSyntax argument when argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) => false,
            ArgumentSyntax { Parent: TupleExpressionSyntax tuple } =>
                !(tuple.Parent is AssignmentExpressionSyntax deconstruction && deconstruction.Left == tuple),
            _ => true,
        };
    }

    private static string? TAuditOwnerRead(
        SemanticModel model, SyntaxNode site, Dictionary<string, TAuditFakeMember> members)
    {
        foreach (SyntaxNode ancestor in site.Ancestors())
        {
            switch (ancestor)
            {
                case BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax:
                case VariableDeclaratorSyntax { Parent.Parent: BaseFieldDeclarationSyntax }:
                    if (model.GetDeclaredSymbol(ancestor) is not ISymbol owner)
                    {
                        return null;
                    }

                    string key = TAuditFakeSymbol.TAuditKeyRead(owner);
                    string? holder = owner.ContainingType is { } type ? TAuditFakeSymbol.TAuditKeyRead(type) : null;
                    bool root = owner is IMethodSymbol { MethodKind: MethodKind.StaticConstructor };
                    return members.ContainsKey(key) || root || holder is null || !members.ContainsKey(holder)
                        ? key
                        : holder;
                case BaseTypeDeclarationSyntax:
                    return null;
            }
        }

        return null;
    }

    private static string TAuditLabelRead(SemanticModel model, SyntaxNode site)
    {
        foreach (SyntaxNode ancestor in site.Ancestors())
        {
            if (ancestor is BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax
                && model.GetDeclaredSymbol(ancestor) is ISymbol owner)
            {
                return $"{owner.ContainingType?.Name}.{owner.Name}";
            }
        }

        return Path.GetFileName(site.SyntaxTree.FilePath);
    }
}
