# Contest

This folder holds the Contesting rule, a field written by both engine and shell.
It also holds the rule's origin and same-lookup walkers and their assays.

## `TAssayContesting`

Assays of the contesting hit and the writers it lists.
The driver field is written once by the engine and then by the shell.
With one member line, body line k is driver line 11 + k.
Pairs whose value travels through an object live in `TAssayContestingCarrier`.
The pair for a surface with generated markup lives in `TAssayContestingMarkup`.
One engine writer and one plain writer give a hit at the plain write, with no "also" text.
A second plain writer is listed after the first as ", also at" its file and line.
A local started from a plain value is plain, and one started from an engine read is engine.
A setter's `value` takes the property's writers.
One lookup fed an engine read and a plain non-constant input still contests.
A literal key does not contest.
A field written only by a copy of itself stays plain.
That assay guards against an over-broad copy rule, so it passes with the rule switched off.
A copy of itself beside an engine writer adds nothing, so the field is engine.
A `ref` hand-off writes the argument the helper assigns, and the helper's own assignment is not counted.

## `TAssayContestingCarrier`

Assays of the contesting hit when the copied value travels through an object.
The carrier is a positional record, an auto property or a constructor parameter.
A literal makes the carried value plain, and an engine read makes it engine.
The record and constructor readers are reached through `Array.ForEach`, a method group, so their parameter stays unseen.
The property reader is never called, so its parameter stays unseen too.
That way only the carrier rule decides each pair.

## `TAssayContestingMarkup`

Assays of the contesting hit for a property of a surface with generated markup.
They hand their own sources to `TAuditBinder.TAuditAssayRun` and walk with `TAuditTruthWalker.TAuditRun`.
The generated half sits at a path under `obj`, where the markup compiler would write it.
So the tracked tree never walks it.
A public property of a type with generated markup has an unseen writer and is plain.
Without the generated half the same property is engine through its C# writer.

## `TAssayOrigin`

Assays of the contesting origin paths that `TAssayContesting` leaves out.
The driver field is written by the engine and then through one origin path.
Beside the blank no-hit assay, a `false` and a zero assay each hit, guarding against over-breadth.
An assay Capsule type sits under the Capsule include, and its read answers by key.
A dependency property with one plain `SetValue` beside an engine one is plain.
A getter body that returns a plain holder is plain.
A method parameter takes every call's argument, so one literal argument makes it plain.
A method also used as a method group has unseen callers, so its parameter stays plain.
Writes of `string.Empty` and `""` beside an engine write are clears, so nothing contests.
A zero or `false` write beside an engine write stays plain, so the blank rule stays narrow.
A Capsule read keyed by an engine value is plain, so it does not contest a plain write.
A Capsule read beside a real engine writer still contests.
With the Capsule rule off that read turns engine, so this fact fails as well.

## `TAuditLookupWalker`

The same-lookup rule of Contesting, kept apart from origin following by role.
When every writer is one shell lookup, the property has one owner.
Contesting then compares the lookup's inputs, not the writers.
The result is null unless every writer calls one and the same shell method or constructor.
The callee is judged by symbol.
A writer may reach the call through holders, as long as every holder writer does.
A holder's writers come from `TAuditOriginHolder.TAuditOriginRead`.
Otherwise it returns the first write with an engine input and every write with a plain input.
A write whose inputs are all keys is neither.
The call scan follows parentheses, casts, null-forgiving marks and holders.
A holder's writers that `TAuditOriginWalker` calls clear are skipped.
It is false when any path ends in something other than such a call.
A gate request is no lookup.
A call's inputs are its arguments, its instance receiver and its initializer values.
A constant input is a key and is skipped.
Any engine input makes the call engine, else any plain input makes it plain.

## `TAuditOriginHolder`

Reads the values a driver holder is written with, for `TAuditOriginWalker` to sort.
A holder's use sites come from `TAuditOriginSite`.
The result is null when the symbol is no holder.
A null entry is an unseen writer that reads plain.
A holder is a field, property, local, constructor parameter or method parameter declared in a shell source.
A method parameter is a by-value parameter of an ordinary method.
A setter's `value` stands for the property's own writes.
A non-private field or property of a type with generated markup code reads as unseen.
Its markup can bind or set it where no C# writer shows.
A dependency property field takes its metadata default and its `SetValue` calls.
A property with a getter body takes what the getter returns.
Returns inside lambdas or local functions are not the getter's.
A positional record property and a constructor parameter take their `new` arguments.
A method parameter takes its arguments at every call of the method.
A local takes its initializer, its loop collection or the value its pattern tests.
An `out` variable or a catch variable is unseen.
Every holder except a getter body or dependency property also takes its assignments.
A `ref` argument into a plain helper writes the arguments the helper assigns to that parameter.
The helper's own assignment to its `ref` parameter yields nothing, since each call site already counts.
A helper is read only when each write to its `ref` parameter copies another by-value parameter of that method.
Any other write to the parameter, or no write at all, makes the write unseen.

## `TAuditOriginSite`

Finds where a holder or a method is used in the shell and Capsule sources.
It reads the values passed or set at those sites for `TAuditOriginHolder`.
The index keys every identifier by its text.
Implicit `new(...)` creations and `this(...)` or `base(...)` calls sit under the key `new`, which no identifier can have.
Binding happens on demand, only for names that match a holder.
`TAuditOriginWalker` rebuilds the index whenever the compilation changes.
A use inside `nameof` is not a use.
A non-neutral `defaultValue` in a dependency property's metadata counts as a writer.
`SetValue` and `SetCurrentValue` write their second argument.
Reads, clears and coercions write nothing.
Any other call that takes the field, such as `SetBinding`, is unseen.
A constructor's sites are its creations and chained calls.
An ordinary method's sites are the invocations of that method in the indexed sources.
Its callers are unseen, as one null, when any caller may be hidden.
That holds when the method is used as a method group or has no seen call.
It holds when the method is generic, `override`, `virtual`, `abstract` or implements an interface member.
A neutral argument, whether given or defaulted, writes nothing.
A neutral value is blank, `false` or zero.
A site whose arguments cannot be read is unseen.

## `TAuditOriginWalker`

Sorts a value written to a judged field by where it comes from, for Contesting.
It follows the value through driver holders to their writers.
Only a member of a logic-side type is an engine source.
It is a class of its own, so the walker's big partial type gains no part.
The verdict of each holder resolved from an empty path is cached for the compilation.
A verdict reached inside another resolve is not cached, since a cycle cut there depends on the path.
An assay binds a new compilation, which empties the verdict cache and rebuilds the site index.
A blank value is `clear`, else the value is `engine` or `plain`.
It is `engine` when it reads a logic member, or calls logic or a reader.
It is also `engine` when it names a logic-typed parameter or local.
It is also `engine` when it names a holder whose origin is engine.
A compound assignment, increment or out argument has no value and is plain.
A name inside `nameof` reads no logic.
A call into the Capsule project is a shell source, and its arguments are not searched.
So a Capsule answer stays the shell's whatever key goes in.
Writers outside the walked roots count, since every tracked shell source is indexed.
A holder is engine when it has a writer that is not a clear and every such writer is engine.
A holder met again on the path is a cycle and reads plain.
A writer that only copies a holder already on the path is skipped as neutral.
Such a copy, as in `held.X = fresh.X`, adds no origin of its own.
A holder written only by such copies has no writer left and stays plain.
Blank means null, default, the empty string and `string.Empty`, and a blank write says nothing about the content.
`false` and zero are not blank, since writing them can be a real shell decision.

## `TAuditTruthContest`

Reports a field written both by the engine and by the shell, as Contesting.
`TAuditTruthWalker` sorts the writes, and this class only weighs them.
Each sorted write carries its reference, its written value and its verdict.
The first engine write stands for the engine side.
Every plain write is a shell writer, kept in reference order.
A clear writes nothing and takes no side.
When both kinds of writer exist, `TAuditLookupWalker.TAuditLookupRead` may sort them again by lookup input.
A field with an engine writer and a shell writer is contesting.
The hit names the engine write by file when that write lies in another file.
The hit lands on the first shell write and lists every other shell write after it.
So a second shell writer can never hide behind the first.

## Stamp

Commit: `90a13c9590ba`
- `TAssayContesting.cs` `977da0d0e60fb28d`
- `TAssayContestingCarrier.cs` `565860d3d28c6128`
- `TAssayContestingMarkup.cs` `e13753e75ac68939`
- `TAssayOrigin.cs` `dedca0570e91b8d3`
- `TAuditLookupWalker.cs` `6d59d41f701cec4c`
- `TAuditOriginHolder.cs` `6992920120af377a`
- `TAuditOriginSite.cs` `0313b780b83a82fa`
- `TAuditOriginWalker.cs` `1de4facff6e9f7ac`
- `TAuditTruthContest.cs` `dc0e43fe54edc327`
