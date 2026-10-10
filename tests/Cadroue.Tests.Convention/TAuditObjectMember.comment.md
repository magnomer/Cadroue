# TAuditObjectMember.cs
Hash: `a16c1d47d7995c6c`

## `internal sealed class TAuditObjectMember(int index, ISymbol symbol, TAuditObjectPart part, bool state, bool mutable)`

One member of a merged type, with the part declaring it and whether it holds state.
`TAuditMemberIndex` is its slot in the union-find that Glued and Fused are read from.
`TAuditMemberState` marks a slot a hub is looked for in.
`TAuditMemberMutable` marks a slot the type can rebind after construction.
`TAuditMemberUses` are the members of the same type its declaration reads, writes or calls.
`TAuditMemberUsers` is the reverse, filled as the uses are.
