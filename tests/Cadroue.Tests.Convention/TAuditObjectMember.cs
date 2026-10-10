using Microsoft.CodeAnalysis;

namespace Convention.Tests;

internal sealed class TAuditObjectMember(int index, ISymbol symbol, TAuditObjectPart part, bool state, bool mutable)
{
    public int TAuditMemberIndex { get; } = index;
    public ISymbol TAuditMemberSymbol { get; } = symbol;
    public TAuditObjectPart TAuditMemberPart { get; } = part;
    public bool TAuditMemberState { get; } = state;
    public bool TAuditMemberMutable { get; } = mutable;
    public HashSet<TAuditObjectMember> TAuditMemberUses { get; } = [];
    public HashSet<TAuditObjectMember> TAuditMemberUsers { get; } = [];
}
