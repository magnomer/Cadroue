using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditFakeCandidate
{
    public static void TAuditMemberScan(SemanticModel model, Dictionary<string, TAuditFakeMember> members)
    {
        string path = TAuditBinder.TAuditRelativeRead(model.SyntaxTree.FilePath);
        foreach (TypeDeclarationSyntax declaration in
                 model.SyntaxTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type || type.TypeKind == TypeKind.Enum)
            {
                continue;
            }

            if (type is { TypeKind: TypeKind.Class, IsStatic: false })
            {
                TAuditMemberAdd(members, type, declaration, path, false);
            }

            if (declaration is RecordDeclarationSyntax { ParameterList: { } parameters })
            {
                foreach (ParameterSyntax parameter in parameters.Parameters)
                {
                    if (type.GetMembers(parameter.Identifier.ValueText).OfType<IPropertySymbol>().FirstOrDefault()
                        is IPropertySymbol property)
                    {
                        TAuditMemberAdd(members, property, parameter, path, true);
                    }
                }
            }

            foreach (MemberDeclarationSyntax member in declaration.Members)
            {
                foreach (ISymbol symbol in TAuditDeclaredRead(model, member))
                {
                    if (TAuditCandidateCheck(symbol))
                    {
                        TAuditMemberAdd(members, symbol, member, path, TAuditStoredCheck(member));
                    }
                }
            }
        }
    }

    private static void TAuditMemberAdd(
        Dictionary<string, TAuditFakeMember> members, ISymbol symbol, SyntaxNode node, string path, bool stored)
    {
        string key = TAuditFakeSymbol.TAuditKeyRead(symbol);
        if (members.ContainsKey(key))
        {
            return;
        }

        int line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        members.Add(key, new TAuditFakeMember(key, symbol.Name, path, line, stored));
    }

    public static void TAuditLineageAdd(Dictionary<string, TAuditFakeMember> members)
    {
        foreach (INamedTypeSymbol type in TAuditBinder.TAuditCompilation
                     .GetSymbolsWithName(_ => true, SymbolFilter.Type)
                     .OfType<INamedTypeSymbol>())
        {
            if (type.BaseType is { } parent
                && members.TryGetValue(TAuditFakeSymbol.TAuditKeyRead(parent), out TAuditFakeMember? inherited)
                && members.ContainsKey(TAuditFakeSymbol.TAuditKeyRead(type)))
            {
                inherited.TAuditMemberReaders.Add(TAuditFakeSymbol.TAuditKeyRead(type));
            }
        }
    }

    private static IEnumerable<ISymbol> TAuditDeclaredRead(SemanticModel model, MemberDeclarationSyntax member)
    {
        return member switch
        {
            BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => [],
            BaseFieldDeclarationSyntax field => field.Declaration.Variables
                .Select(variable => model.GetDeclaredSymbol(variable))
                .OfType<ISymbol>(),
            _ => model.GetDeclaredSymbol(member) is ISymbol symbol ? [symbol] : [],
        };
    }

    private static bool TAuditStoredCheck(MemberDeclarationSyntax member)
    {
        return member switch
        {
            BaseFieldDeclarationSyntax => true,
            PropertyDeclarationSyntax { ExpressionBody: null, AccessorList: { } accessors } =>
                accessors.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null),
            _ => false,
        };
    }

    private static bool TAuditCandidateCheck(ISymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared || symbol.IsOverride || symbol.IsExtern)
        {
            return false;
        }

        bool kind = symbol switch
        {
            IMethodSymbol method => method.MethodKind == MethodKind.Ordinary
                && !(method.IsStatic && method.Name == "Main"),
            IPropertySymbol property => !property.IsIndexer && property.ExplicitInterfaceImplementations.Length == 0,
            IEventSymbol eventSymbol => eventSymbol.ExplicitInterfaceImplementations.Length == 0,
            IFieldSymbol field => field.AssociatedSymbol is null,
            _ => false,
        };
        return kind && TAuditFakeSymbol.TAuditContractRead(symbol).Count == 0;
    }
}
