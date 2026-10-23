# Strict

This folder holds the surface audit with its rules, conditions, meddling judge, setting, ledger and walker.
It also holds the reach and host walkers the audit runs.
The contract walker and its assays sit in the Contract folder.

## `TAuditHostWalker`

Holds Host to construction and wiring, since Host holds no behaviour.
A statement passes as a declaration, an expression statement, a block-bodied local function or a closing `return`.
An expression passes as a name, a literal, a member chain, a call or a construction.
A simple assignment, an `await` and a lambda pass too.
Anything else is an overworking hit on its line, with every statement nested in it.
One hit is reported per line.
The scan covers the top-level statements and every member body of one tree.
An expression body counts for a method, a property and an indexer alike.
A property accessor with a body is behaviour and counts whole.
A `return` passes only as the block's last statement, so an early return is a hit.
A branch, an operator, a pattern, a cast or a conditional access is a hit.
A `ref`, `in` or `out` argument is a hit.

## `TAuditReachWalker`

Parses the surface markup as XML and looks at every element, attribute and text node.
A name below the driver is an overreaching hit, and a trigger or a computing binding is a branching hit.
A line through which logic reaches the markup is one tethering hit, whatever the layer it reaches.
A name the logic patterns match is a logic name, wherever it stands in a value.
A name only the Deportment namespace declares is the driver's own and is not overreaching.
A binding without a logic name, such as `{Binding}`, raises no overreaching hit.
The value scan reports only names the logic patterns match.
A file that does not parse is itself a hit, since a walk that cannot read it sees nothing.
The tethering markers of a file are gathered by line and reported one hit per line, markers joined.
An element line is marked for a binding element, a tethering slot property element, or a converter or selector.
An attribute line is marked for a tethering slot or a listed extension.
It is also marked for an extension whose prefix maps a code namespace.
A `Setter` whose `Property` names a tethering slot marks its line with that slot.
A literal value of a tethering-literal attribute, or of a `Setter` naming one, marks its line too.
An `x:Class` naming a type outside the surface namespace marks its line.
An `x:Class` naming a surface type is the page's shell and is allowed.
An element resolves to a type through its `clr-namespace` or the namespaces its XML namespace defines.
It is tethering when that type or a base holds a listed type among itself and its interfaces.
A property element is never a type and is skipped.
The XML namespaces come from the `XmlnsDefinitionAttribute` of every reference and of the source itself.
The control read lists every `x:Name` or `Name` value the markup declares.
A file that does not parse is skipped there and reported by the reach walk.
The branching scan runs first, then every attribute but `x:Class` and every text node of one element.
A namespace declaration goes to the namespace scan, the rest to the value scan.
A trigger element is one hit.
Each computing slot in an attribute, a value or a property element is one more.
An `xmlns` mapping a logic namespace is one hit.
A logic constant read is one hit.
So is each other logic name in the value, with the slot named.
A name found in the Deportment set is skipped.

## `TAuditStrict`

These are the surface rules.
A surface is what the user sees, and a surface member only calls a function.
A surface holds no state, never branches or computes, and names only its driver.
The rules hold every surface project alike, and Host is held to construction and wiring.
A surface type holds no member but a constructor, and its markup holds no hook into logic.
Each fact holds one hit kind against the ledger.
While `TAuditStrictEnforced` is false every hit-kind fact passes and only logs its count.
Enforced, a kind fails in any file whose count stands above its ledger ceiling.
The ledger keys each ceiling by file, so each medium's lag is its own.
A new file starts at zero.
The walk runs once and every fact reads the same hits, surface types and walked sources.
The report is written once, on the first hit-kind fact that runs.
A surface field, event field, auto-property or primary constructor parameter is hoarding.
A constant is no state and is left out.
A `DependencyProperty` or `RoutedEvent` static counts, since the surface allows no exception.
A mutable static field in a driver type is state shared past any one gate.
A surface line that is not a plain call is meddling.
That includes a branch, a loop, an operator, an assignment or a declaration.
A LINQ query is a breach too, since a filter decides what is drawn.
Every line inside a breaching statement counts, so logic added inside one moves the count.
A surface line naming a type from below the driver is prying.
It overlaps the border audit's `Undercutting` count, which counts the same names per ring pair.
A markup line is overreaching when it maps a namespace below the driver.
It is also overreaching when it reads a constant from there or names a member there.
A markup trigger or visual state, or a binding slot that converts, formats, selects or validates, is branching.
Each is a branch or a computation standing in the surface.
A markup line through which logic reaches the markup is tethering.
That covers a binding, a command, an input binding or a code reach.
A code reach is `x:Static` or a markup extension whose prefix maps a code namespace.
A command parameter or target, a display member path or a selected value path is tethering too.
So is a literal set in a tethering-literal slot, since code reads it as a value to branch on.
Each counts as an attribute or as the property a `Setter` names.
A converter or a template or style selector declared in the markup is tethering too.
Each line counts once, however many markers it holds.
`{StaticResource}`, `{DynamicResource}` and `{x:Type}` stay allowed.
A surface member that is not a constructor is freelancing, whether a method, property, event, indexer or operator.
The shell only constructs, so every handler or helper belongs in Deportment.
Generated code is left out, as for every surface fact.
A surface identifier holding a character outside ASCII is a homoglyph.
A look-alike glyph would let a name pass every prefix rule while reading as another.
A Host line that does more than construct and wire is overworking.
That means a branch, a choice, an operator or an early return.
Host holds no behaviour, so each such line belongs in the layer that owns the decision.
A driver line holding a pack URI or naming the Veneer assembly is hardwiring.
Deportment pulls every part by contract ID and never names the Veneer.
The line counts once, under the first marker it holds.
A driver type deriving from a scaffold type, such as a window, a page or a resource dictionary, is masquerading.
The Veneer holds the scaffold, so such a type is scaffold standing in the wrong layer.
Each partial part counts in its own file.
A contract ID passed to the contract type that no `x:Name` or `x:Key` in the surface markup declares is dangling.
An ID that is not a constant counts too, since no markup can be checked against it.
Every string argument of a call on the contract type is an ID.
A driver line that names a file system type, outside the exempt files, is disk work.
A driver holds only what its medium needs, and disk work stays behind the engine's ports.
The console's reader and writer live in `System.IO` too, so the namespace alone is no hit.
A surface line is catalog work when it reads a file, parses JSON or runs a regex.
Starting a process or a task is catalog work too.
Each of those is work the engine does, and the surface only asks for the answer.
A file whose stream use is the framework's own may be exempt by name.
The disk and catalog facts assert zero hits and keep no ledger.
They scan through `TAuditStrictSource`.
Every exempt file still holds a line its exemption spares, so a stale exemption is removed.
The core read of a condition under a prefix operator returns within a time bound.
It must not loop on the operator.
A branch in an expression-bodied indexer or local function is a host breach.
Every ledger ceiling equals its count, so a ceiling left above the count is stale.
The lowered ledger is written under `temp/audit` to copy over the tracked one.
Every tracked surface and host source must reach the walkers, so a file the binder dropped does not pass unseen.
The UI sources, host sources, surface markup and driver sources are enumerated with Git.
It fails when the UI or host set is empty, since the audit would then pass vacuously.
An empty markup set passes, since a surface may hold no markup.
The four walkers run once, with paths made repo-relative.
The contract walker reads the driver sources and the surface markup together.
The report opens with the counts, then a per-type table, then every hit by kind.

## `TAuditStrictCondition`

Shared syntax predicates on conditions, patterns and their operands.
The moonlighting and laundering walkers, the misfiring judge and the truth sink read them.
The truth control and the strict audit's core-read fact read them too.
So no single walker owns them.
The core of a condition is unwrapped from parentheses and negation.
Any other prefix operator stops the unwrapping and is returned as the core.
A pattern passes for a null constant, its negation, an empty property pattern or a type pattern.
A declaration or `var` pattern that only names the value also passes.
A null check is the `null` or `default` literal.
A node is data when any name inside it resolves below the conduct layer.
The name is judged by its symbol or by its type.
A conduct-layer value is the driver's to reshape, so it is not data here.

## `TAuditStrictLedger`

The surface audit's ceilings, by kind and then by repo-relative file.
Every kind the surface audit counts reads its ceilings from this one ledger.
A kind the ledger omits has ceilings of zero.
Each ceiling is the count of that kind in that file, and may only fall.
A file missing from a kind has a ceiling of zero, so a new file starts clean.
`TAuditRatchet` holds every ceiling against the committed copy, and a new generation baselines it afresh.
When a count falls, the ledger fact writes the lowered ledger under `temp/audit` to copy here.

## `TAuditStrictMeddling`

Judges each member of a surface type against the one rule that only a call may stand.
`TAuditStrictWalker` hands it every member of a surface part.
It walks bodies down to their operands and collects every breach.
One hit is kept per line of a surface member that is not a plain call.
Every statement nested in a breach is a breach of its own line, so a long branch counts in full.
The hit names the syntax kind that broke the rule.
The bodies of a member are its block, arrow, accessor bodies and a constructor initializer's arguments.
A field or property initializer is a body too, so a lambda there cannot hide its logic.
A block must hold only call statements, and an arrow, lambda body or initializer must be one call.
A statement passes as a call statement or as a `return` of a call.
Any other statement is a breach, whether a branch, a loop, a declaration or a `try`.
The expression must be a call on a name or a plain member chain, with plain arguments.
A call to a method of a query type is a breach.
A `ref`, `in` or `out` argument is a breach.
A plain operand is a name, `this`, `base`, a predefined type, a literal or a member chain.
A call or a lambda is plain too, and a lambda is judged by its own body.
An operator, an assignment, a `new`, a cast, an `await` or a `?.` is a breach.

## `TAuditStrictSetting`

Hand-written and tracked.
The surface-audit switch, the report path and the classifiers live here.
The per-file ceilings live in the ledger named by `TAuditLedgerFile`.
No script writes this file.
The enforced switch false makes every hit-kind fact pass and only log its count.
True fails a fact on any file whose count of a kind stands above its ledger ceiling.
The report path carries the version.
The Veneer include patterns select the surface sources.
A type declared under one of them is a surface type, whatever its base.
The Deportment include patterns are scanned for a reach into the file system.
The host include patterns are held to construction and wiring.
The Deportment namespace holds the types and members a markup binding may name.
A name a type below the cut also declares is left out.
So a deeper binding cannot pass by coincidence.
An `x:Class` in the surface markup naming a type outside the Veneer namespace is tethering.
The contract type is the driver's only door to the scaffold.
Every string argument of a call on it is a contract ID.
The hardwiring markers match as plain text, string literals included, since a pack URI is a literal.
A driver type with one of the masquerading types among its base types is masquerading.
The contract ID attributes are read in any XML namespace.
`Name` covers `x:Name` and the framework's own `Name`.
The query types are those whose extension methods are queries, each a decision a surface may not make.
A catalog pattern marks file, JSON, regex or process work, or a started task, in the surface.
That work belongs below the UI, in the engine.
The overreaching namespaces are those below the driver that markup may not map.
A mapping lets a binding reach them.
The branching slots convert, format, fall back, select a template or style, or validate.
Each is a computation in the markup, whether set as an attribute, inside an extension or as a property element.
The tethering slots tether logic as an attribute, a property element or through a `Setter`.
The command slots, `DisplayMemberPath` and `SelectedValuePath` all name what code reads.
The tethering literals tether only when set to a plain literal, directly or through a `Setter`.
Code branches on such a literal, so it tethers.
A value opening a markup extension is left to the extension rules.
The script reads the same list from `tetheringLiterals` in `AuditUI.json`.
The tethering extensions bind to logic or read a code member.
A markup extension whose prefix maps a code namespace is tethering without being listed.
The tethering types are converter and selector types a markup element may not be.
An element is one when its type or a base type is listed or implements a listed interface.

## `TAuditStrictSource`

The line scan behind the disk and catalog facts.
It enumerates the tracked files matching an include list.
The scope takes the name audit's excluded segments, suffixes and prefixes.
A file whose name is exempt is skipped.
String literals and line comments are blanked first, so a word inside a message is not a hit.
Each line is tested against every forbidden pattern.
A hit reads `path:line pattern`, with a repo-relative path and forward slashes.
`TAuditExemptRead` scans with no exemption and lists each exempt name that holds no hit.
So a stale exemption shows up and is removed.

## `TAuditStrictWalker`

Applies the surface rules to the UI sources by symbol.
A type is a surface by its folder alone, since a driver uses its medium freely.
Partial types are joined by symbol, and one surface part makes the whole type a surface.
Every type of the UI sources is grouped, then scanned for hoarding, meddling, prying and freelancing per surface type.
The meddling scan of each member lives in `TAuditStrictMeddling`.
Every surface file is also scanned for homoglyphs, enums and delegates.
Outside the surfaces only a mutable static field is a hit.
The surface type names come back for the report.
Enum and delegate declarations of a surface file are read separately, since no type scan reaches them.
A name they carry from below the driver is a prying hit.
An enum value that is not a literal is a computation.
Every field, event field, auto-property and primary constructor parameter of a surface type is hoarding.
A `readonly` or a wired `null!` field counts too, and a constant does not.
In a driver type, every mutable static field is a sharing hit.
One freelancing hit is kept per method, operator, property, event or indexer the surface part declares.
A constructor is the one member the shell keeps.
A property whose accessors have no body stores a value.
One prying hit is kept per line of a surface declaration naming a type from below the driver.
Nested types are left to their own scan.
One homoglyph hit is kept per identifier holding a character outside ASCII.
The surface walk and the driver walk each count it under their own audit.

## Stamp

Commit: `90a13c9590ba`
- `TAuditHostWalker.cs` `2a48c5d2dab0f8e1`
- `TAuditReachWalker.cs` `e54546f894fc3f86`
- `TAuditStrict.cs` `1241dceb30a2881a`
- `TAuditStrictCondition.cs` `920bd6f4febc550a`
- `TAuditStrictLedger.json` `b964a1e8c46e04b7`
- `TAuditStrictMeddling.cs` `dd697aec78b53c98`
- `TAuditStrictSetting.cs` `8015b431cc8f89f5`
- `TAuditStrictSource.cs` `7b7ff6d00aa27bff`
- `TAuditStrictWalker.cs` `723eb9e5bd2465ff`
