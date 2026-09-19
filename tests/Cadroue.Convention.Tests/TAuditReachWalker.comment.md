# TAuditReachWalker.cs

## `internal static class TAuditReachWalker`

Parses the shell markup as XML and looks at every attribute and text node for a reach into logic.
A name that starts with `L` and a capital is a logic name, wherever it stands in a value.

## `private static readonly Regex TAuditLogicPattern`

A logic name standing alone inside a value, whether a binding path, a parameter or a resource key.

## `private static readonly Regex TAuditStaticPattern`

The logic type an `x:Static` reads.

## `public static IReadOnlyList<TViolation> TAuditRun(IEnumerable<string> markupPaths)`

Loads every markup file with line numbers and scans each element.
A file that does not parse is itself a hit, since a walk that cannot read it sees nothing.

## `public static IReadOnlyList<string> TAuditControlRead(IEnumerable<string> markupPaths)`

Every `x:Name` the markup declares, so the code walkers know which identifier is a control.
A file that does not parse is skipped here and reported by the reach walk.

## `private static void TAuditElementScan(string path, XElement element, List<TViolation> violations)`

Every attribute but `x:Class` and every text node of one element.
A namespace declaration goes to the namespace scan, the rest to the value scan.

## `private static void TAuditNamespaceScan(string path, int line, string value, List<TViolation> violations)`

One hit per `xmlns` mapping a logic namespace.

## `private static void TAuditValueScan(string path, int line, string slot, string value, List<TViolation> violations)`

One hit per logic constant read and one per other logic name in the value, with the slot named.
