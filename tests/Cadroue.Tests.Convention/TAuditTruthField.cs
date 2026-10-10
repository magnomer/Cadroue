using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal sealed record TAuditTruthField(
    HashSet<ISymbol> TFieldSymbols,
    string TFieldName,
    ITypeSymbol TFieldType,
    string TFieldPath,
    int TFieldLine,
    bool TFieldShared)
{
    internal static IEnumerable<TAuditTruthField> TAuditFieldRead(IReadOnlyList<TypeDeclarationSyntax> type)
    {
        foreach (TypeDeclarationSyntax part in type.SelectMany(part =>
                     part.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()))
        {
            foreach (FieldDeclarationSyntax field in part.Members.OfType<FieldDeclarationSyntax>())
            {
                bool fixture = field.Modifiers.Any(modifier =>
                    modifier.IsKind(SyntaxKind.ReadOnlyKeyword)
                    || modifier.IsKind(SyntaxKind.ConstKeyword));
                foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                {
                    if (TAuditBinderSymbol.TAuditSymbolRead(variable) is not IFieldSymbol symbol
                        || TAuditTruthWalker.TAuditHandleCheck(symbol.Type))
                    {
                        continue;
                    }

                    HashSet<ISymbol> symbols = TAuditTruthWalker.TAuditAliasRead(symbol, type);
                    if (!fixture || TAuditBinderSide.TAuditEngineCheck(symbol.Type) || TAuditFillCheck(symbols, type))
                    {
                        yield return new TAuditTruthField(
                            symbols,
                            TAuditBinderSymbol.TAuditLabelRead(symbol),
                            symbol.Type,
                            field.SyntaxTree.FilePath,
                            TAuditTruthReference.TAuditLineRead(variable),
                            symbol.IsStatic);
                    }
                }
            }

            foreach (ParameterSyntax parameter in part.ParameterList?.Parameters ?? [])
            {
                if (TAuditBinderSymbol.TAuditSymbolRead(parameter) is not IParameterSymbol symbol
                    || TAuditTruthWalker.TAuditHandleCheck(symbol.Type))
                {
                    continue;
                }

                HashSet<ISymbol> symbols = new([symbol], SymbolEqualityComparer.Default);
                foreach (IPropertySymbol property in symbol.ContainingType?.GetMembers(symbol.Name)
                             .OfType<IPropertySymbol>() ?? [])
                {
                    symbols.Add(property.OriginalDefinition);
                }

                yield return new TAuditTruthField(
                    symbols,
                    TAuditBinderSymbol.TAuditLabelRead(symbol),
                    symbol.Type,
                    parameter.SyntaxTree.FilePath,
                    TAuditTruthReference.TAuditLineRead(parameter),
                    true);
            }

            foreach (PropertyDeclarationSyntax property in part.Members.OfType<PropertyDeclarationSyntax>())
            {
                bool settable = property.AccessorList?.Accessors.Any(accessor =>
                    accessor.IsKind(SyntaxKind.SetAccessorDeclaration)
                    || accessor.IsKind(SyntaxKind.InitAccessorDeclaration)) == true;
                if (!settable
                    || TAuditBinderSymbol.TAuditSymbolRead(property) is not IPropertySymbol symbol
                    || TAuditTruthWalker.TAuditHandleCheck(symbol.Type))
                {
                    continue;
                }

                yield return new TAuditTruthField(
                    new HashSet<ISymbol>([symbol], SymbolEqualityComparer.Default),
                    TAuditBinderSymbol.TAuditLabelRead(symbol),
                    symbol.Type,
                    property.SyntaxTree.FilePath,
                    TAuditTruthReference.TAuditLineRead(property),
                    true);
            }
        }
    }

    private static bool TAuditFillCheck(HashSet<ISymbol> symbols, IReadOnlyList<TypeDeclarationSyntax> type)
    {
        return TAuditTruthReference.TAuditUseRead(symbols)
            .Where(identifier => TAuditTruthReference.TAuditInsideCheck(identifier, type))
            .Select(TAuditTruthReference.TAuditReferenceRead)
            .Any(reference => TAuditTruthReference.TAuditWriteCheck(reference, out _));
    }
}
