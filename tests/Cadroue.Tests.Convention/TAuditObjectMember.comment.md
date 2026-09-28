# TAuditObjectMember.cs

## `internal sealed class TAuditObjectMember(int index, ISymbol symbol, TAuditObjectPart part, bool state)`

One member of a merged type, with the part declaring it and whether it holds state.
`TAuditMemberIndex` is its slot in the union-find the weave is read from.
`TAuditMemberUses` are the members of the same type its body reads, writes or calls.
`TAuditMemberUsers` is the reverse, filled as the uses are.
