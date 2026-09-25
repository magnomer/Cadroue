# TAuditReachWalker.cs

## `internal static class TAuditReachWalker`

Parses the veneer markup as XML and looks at every element, attribute and text node.
A reach into logic is a Reach hit, and a trigger or a computing binding is a Trigger hit.
A name that starts with `L` and a capital is a logic name, wherever it stands in a value.
A name the Deportment namespace declares is the shell's own state and is not a reach.

## `private static readonly Regex TAuditLogicPattern`

A logic name standing alone inside a value, whether a binding path, a parameter or a resource key.

## `private static readonly Regex TAuditStaticPattern`

The logic type an `x:Static` reads.

## `private static readonly Regex TAuditSlotPattern`

A computing slot set inside a markup extension, such as a binding's `StringFormat=`.

## `public static IReadOnlyList<TViolation> TAuditRun(IEnumerable<string> markupPaths)`

Loads every markup file with line numbers and scans each element.
A file that does not parse is itself a hit, since a walk that cannot read it sees nothing.

## `public static IReadOnlyList<string> TAuditControlRead(IEnumerable<string> markupPaths)`

Every `x:Name` the markup declares, so the code walkers know which identifier is a control.
A file that does not parse is skipped here and reported by the reach walk.

## `private static void TAuditElementScan(string path, XElement element, IReadOnlySet<string> deportment, List<TViolation> violations)`

The trigger scan first, then every attribute but `x:Class` and every text node of one element.
A namespace declaration goes to the namespace scan, the rest to the value scan.

## `private static void TAuditTriggerScan(string path, XElement element, List<TViolation> violations)`

One hit for a trigger element, and one per computing slot in an attribute or a value.

## `private static void TAuditNamespaceScan(string path, int line, string value, List<TViolation> violations)`

One hit per `xmlns` mapping a logic namespace.

## `private static void TAuditValueScan(string path, int line, string slot, string value, IReadOnlySet<string> deportment, List<TViolation> violations)`

One hit per logic constant read and one per other logic name in the value, with the slot named.
A name found in the Deportment set is skipped.
