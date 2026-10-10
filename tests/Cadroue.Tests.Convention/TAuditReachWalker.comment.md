# TAuditReachWalker.cs
Hash: `e54546f894fc3f86`

## `internal static class TAuditReachWalker`

Parses the surface markup as XML and looks at every element, attribute and text node.
A name below the driver is an overreaching hit, and a trigger or a computing binding is a branching hit.
A line through which logic reaches the markup is one tethering hit, whatever the layer it reaches.
A name that starts with `L` and a capital is a logic name, wherever it stands in a value.
A name only the Deportment namespace declares is the driver's own and is not overreaching.
A binding without a logic name, such as `{Binding}`, raises no overreaching hit.
`TAuditValueScan` reports only names the logic patterns match.

## `private static readonly Regex TAuditLogicPattern`

A logic name standing alone inside a value, whether a binding path, a parameter or a resource key.

## `private static readonly Regex TAuditStaticPattern`

The logic type an `x:Static` reads.

## `private static readonly Regex TAuditSlotPattern`

A computing slot set inside a markup extension, such as a binding's `StringFormat=`.

## `private static readonly Regex TAuditTetheringPattern`

The opening of a markup extension, with its prefix apart from its name.

## `public static IReadOnlyList<TViolation> TAuditRun(IEnumerable<string> markupPaths)`

Loads every markup file with line numbers and scans each element.
A file that does not parse is itself a hit, since a walk that cannot read it sees nothing.
The tethering markers of a file are gathered by line and reported one hit per line, markers joined.

## `private static void TAuditTetheringScan(XElement element, Dictionary<string, List<string>> spaces, SortedDictionary<int, List<string>> hooks)`

Marks the element line for a binding element, a tethering slot property element, or a converter or selector.
Marks an attribute line for a tethering slot, a listed extension, or an extension whose prefix maps a code namespace.
A `Setter` whose `Property` names a tethering slot marks its line with that slot.
A literal value of a tethering-literal attribute, or of a `Setter` naming one, marks its line too.
An `x:Class` naming a type outside the surface namespace marks its line.
An `x:Class` naming a surface type is the page's shell and is allowed.

## `private static void TAuditTetheringAdd(SortedDictionary<int, List<string>> hooks, int line, string marker)`

Adds one marker under its line.

## `private static bool TAuditTetheringCheck(XElement element, Dictionary<string, List<string>> spaces)`

Resolves the element to a type through its `clr-namespace` or the namespaces its XML namespace defines.
It is tethering when that type or a base holds a listed type among itself and its interfaces.
A property element is never a type and is skipped.

## `private static Dictionary<string, List<string>> TAuditSpaceRead()`

Every XML namespace the bound assemblies define, with the code namespaces it maps.
It reads the `XmlnsDefinitionAttribute` of every reference and of the source itself.

## `public static IReadOnlyList<string> TAuditControlRead(IEnumerable<string> markupPaths)`

Every `x:Name` the markup declares, so the code walkers know which identifier is a control.
A file that does not parse is skipped here and reported by the reach walk.

## `private static void TAuditElementScan(string path, XElement element, IReadOnlySet<string> deportment, List<TViolation> violations)`

The branching scan first, then every attribute but `x:Class` and every text node of one element.
A namespace declaration goes to the namespace scan, the rest to the value scan.

## `private static void TAuditBranchingScan(string path, XElement element, List<TViolation> violations)`

One hit for a trigger element, and one per computing slot in an attribute, a value or a property element.

## `private static void TAuditNamespaceScan(string path, int line, string value, List<TViolation> violations)`

One hit per `xmlns` mapping a logic namespace.

## `private static void TAuditValueScan(string path, int line, string slot, string value, IReadOnlySet<string> deportment, List<TViolation> violations)`

One hit per logic constant read and one per other logic name in the value, with the slot named.
A name found in the Deportment set is skipped.
