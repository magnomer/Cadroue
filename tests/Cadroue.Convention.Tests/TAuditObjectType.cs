using Microsoft.CodeAnalysis;

namespace Convention.Tests;

internal sealed class TAuditObjectType(INamedTypeSymbol symbol)
{
    public INamedTypeSymbol TAuditTypeSymbol { get; } = symbol;
    public List<TAuditObjectPart> TAuditTypeParts { get; } = [];
    public Dictionary<ISymbol, TAuditObjectMember> TAuditTypeMembers { get; } = new(SymbolEqualityComparer.Default);
}
