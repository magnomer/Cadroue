# TAuditFakeMember.cs

## `internal sealed class TAuditFakeMember(string key, string name, string path, int line, bool stored)`

One source member the fake audit judges, keyed by its documentation id.
`TAuditMemberStored` marks a field, a field-like event or an auto-property, which a write alone never uses.
`TAuditMemberReaders` holds the keys of the source members that read it, or an empty key for a live root.
`TAuditMemberTesters` names the test members that read it.
`TAuditMemberLive` is set once a live reader reaches it.
