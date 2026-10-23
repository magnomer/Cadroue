# Assay

This folder holds assays of the driver walker's core, sink, relay, control and zeroing checks on hand-written sources.
It also holds the walker's sink and relay halves, beside the assays that check them.

## `TAssayControl`

Assays of the Puppeteering kind, run through `TAuditTruthWalker.TAuditRun` with the stand-in gate of `TAssayTruth`.
Only hits of that kind are kept.
The check adds a Conduct record beside the gate, so reading its property crosses no gate.
A control type calling a gate directly and through a driver relay reports both members as puppeteering.
The relay type itself is no control, so its own request is not puppeteering.
A control reading a Conduct record property makes no request.
A control raising its own event, which a driver handles with a gate call, reports nothing.
The surface signals and the driver requests, so the event is no puppet string.
It fails when events join the relay set before the puppeteering snapshot is taken.

## `TAssayRelay`

Assays of the relay reading, meaning which delegates and members count as a request.
Each gatekeeping assay tests a field in a branch that calls a delegate.
The hit appears only when the delegate is a relay.
The two-type assays hand a whole source, so the injected delegate crosses from one type to another.
A gate delegate passed to another type's constructor and stored there is a relay when invoked.
A delegate that reaches no gate makes no relay, and such twins only guard against an over-broad rule.
A local holding a gate delegate is a relay, and so is a local initialized from another relay local.
A local assigned a gate delegate after its declaration is a relay.
A field assigned a gate delegate in a method is a relay.
A field initializer holding one is not, since only assignments are read as wiring.
A gate delegate wired to an interface event makes that event a relay.
Raising the event that implements it is then a request.
That twin fails when either half of the interface rule is switched off.
The gatekeeping check adds the interface as a third source in the Conduct folder, outside the shell.
So only the interface clause of the wiring rule can make its event a relay.

## `TAssaySink`

Assays of the sink half of the driver walker, run through `TAuditTruthWalker.TAuditRun` as `TAssayTruth` does.
They share its driver path, helpers and stand-in gate.
A `break` in a branch leaves its switch section, so a request after the branch in that section is gated.
The hit sits on the field read in the branch condition, driver line 15.
A request only in a later case is not skipped by the `break`.
A `continue` in a branch skips the rest of its loop, so a later request in that loop is gated.
A `continue` scopes to its loop, so a request after the loop is not gated by it.
A condition chaining a presence test with a value test decides the request.
A chain of presence tests alone is still a presence check.
A field read through parentheses and `!` and compared as a value decides the request.
The same field wrapped that way is still a receiver, so reading its member is no sink.
A field read in a branch condition decides the request.
A field named only inside `nameof` is not read.
A field passed by `ref` is followed into the callee's parameter.
The hit sits on the parameter read in the callee's branch, reported through the ref.
A field passed by value is not followed.
A sibling that gates on its owner's non-private alias of a field is gatekeeping on that field.
The hit sits on the sibling's branch, reported against the owner's field.
An owner that keeps the field private and exposes only a method gives the sibling nothing to read.

## `TAssayTruth`

Assays of the driver walker, run through `TAuditTruthWalker.TAuditRun`.
`TAuditTruth` runs the same walker over the tree.
Its helpers and stand-in gate are internal, so sibling truth assays such as `TAssaySink` and `TAssayRelay` reuse them.
The assayed driver sits under a driver root so the walker reads it.
The stand-in gate sits under Conduct so its type takes the Conduct side.
The gate has three instance methods, so each call to any is a send.
`CAssayApply` returns nothing and `CAssayRead` returns an int, so a ternary or an assignment can take its answer.
`CAssayRemove` takes a nullable id, so a Zeroing assay can send a literal zero or a null.
A body is wrapped as the body of `QAssayRun`, starting on line 10.
So its line k is line 9 + k.
Every Misfiring case here sends first on body line 3, so the reason names line 12.
A wrapper with members starts them on line 8 and the method follows after one blank line.
So with m member lines, body line k is driver line 10 + m + k.
A check holds the hits of one kind against the expected hit, or against none.
The expected hit names the driver path, line, member, kind and reason.
Hits of other kinds are left to their own assays.
Hit paths are made repo-relative, as the tree audit makes them.

Whether a branch leaves, so a later send is exclusive, is pinned in pairs.
A bare `return`, a bare `throw` or a `break` or `continue` directly in the branch leaves it.
The `break` and `continue` may sit in a loop that lies outside the branch.
A `return` under a nested `if` may not run, so the branch does not leave.
A `return` in a lambda or anonymous method leaves only that function.
A `throw` in a local function runs only if the function is called.
A `throw` expression such as `text ?? throw` is not a jump.
A `break` or `continue` whose loop or switch lies inside the branch stays in the branch.

## `TAssayZeroing`

Assays of the Zeroing kind, run through `TAssayTruth.TAssayTruthCheck` and its driver wrapper.
Zeroing is pinned in pairs of a literal zero and a `null`.
A literal zero passed to a driver helper whose id reaches the gate is Zeroing.
The helper forwards its id to `CAssayRemove`, so its id position is hot.
It is four member lines, so body line k of a driver holding it is line 14 + k.
The hit is named after the helper and lands on the literal's line.
A `null` at that position is no Zeroing, since absence is spelled in the type.
A zero handed to a driver constructor is Zeroing when the stored id later reaches the gate.
The driver type is appended after `QAssay` and stores its id into a getter-only property in its constructor.
Appended text leaves every line of `QAssay` where it was.
That needs the wider Zeroing map, since the hot map holds no constructor.
A stored id compared with zero is Zeroing when read inside a hot argument.
The hit is named after the compared member.
A zero passed to a helper that never reaches a gate is no Zeroing.
So a medium value such as a width stays free.

## TAuditTruthRelay

The relay half of the truth walker, covering which driver members stand in for logic and at which parameters.
Every set is keyed by symbol, so two members sharing a name never share a verdict.
The relay names are the driver members whose body requests logic, so a call to one is a request.
A local function, a delegate field, a delegate property and an event count too.
Invoking one of those is then a request, so a request behind a delegate cannot hide.
The puppet names are the relay set as it stands before any event joins it.
An event is a signal, and the member that handles it is the one that requests.
So a control raising its own event, or forwarding a child's, is no puppeteer.
The puppet set stays null while the first pass grows, which keeps events out of the wiring.
The reader names are the driver members whose body reads or requests logic.
So a value from one comes from logic.
A request is a read too, so a relay is read through as well.
The hot names give, for each method and local function, the positions of the parameters that reach logic inside it.
An argument at any other position is shown, not sent, and is no sink.
The Zeroing map is a copy of the hot names grown further.
Constructors join, and a parameter stored into a member read inside a hot argument is hot too.
It is a separate dictionary, so every other kind keeps the narrower map unchanged.
The Zeroing reads are the driver fields and properties read inside an argument at a Zeroing hot position.
Only members of a shell type count, so a framework count or text never joins.
A literal zero compared with one of them is a hit.
A delegate member becomes a relay when a request, a relay or logic is assigned or subscribed to it.
Only delegate-typed members of a shell type count, so a framework property never becomes a relay.
An interface event counts too, since a shell type raises it through its own implementation.
A delegate local becomes a relay when its initialiser or a later assignment is one.
A delegate parameter stored in a member or local passes on what its callers hand it.
Wiring, locals and seams are read over every tracked source, not only the walked drivers.
So a composition root that hands a gate to a driver constructor is traced too.
Every set and the hot positions grow to a fixed point, so a chain of wrappers is followed through.
The first fixed point leaves events out and is kept as the puppet names.
Growth then resumes with events, so every other set reaches the same end.
The Zeroing map is built last, from the finished relay set and hot positions.
The seams group every argument handed to a delegate parameter by that parameter.
They read every tracked source, so a caller in another type or project counts too.
That lets a stored parameter be judged by what its callers pass.
A bare delegate parameter is a relay when any caller hands it one, followed through every hop.
A parameter is visited once, so a cycle of callers ends.
Only a stored parameter is judged this way.
A parameter only invoked in place stays plain, since its caller's lambda already requests there.
An argument binds to its parameter by name when named, else by position.
A call, a construction, a `base` or `this` initialiser and a primary base call all bind.
It is null when the call does not resolve to a method.
A value handed to a delegate member requests when it names a relay or a logic method as a group.
Naming a relay member or local without calling it counts as well.
A parameter is hot when the body passes it to logic or a hot relay position.
Passing a member of it counts too.
A method and a local function are read alike.
The read reports whether a new position was found, so the caller loops again.
The Zeroing build starts from a copy of the current hot positions.
Methods, constructors, operators and local functions may hold hot parameters.
A parameter is also hot when stored into a field or property that is read inside a hot argument.
A store into a field counts when a getter alias of it is read, so a backing field is followed.
The map and the read members grow together to a fixed point.
So a constructor whose stored id later reaches a gate gets a hot position.
The hot names are only copied, never written, so Replaying and the other kinds see no change.
A store gives the stored member and its getter aliases.
It gives an empty set when the target is no field or property.
A field's aliases come from the alias read over its own type's parts only.
A property stands alone.
Each non-empty answer is cached by symbol, since the fixed point asks for the same store many times.
A member reads when its body reads a logic member or calls a reader.
A name inside `nameof` does not count.

## TAuditTruthSink

The sink half of the truth walker, covering where a driver value may not go.

`TAuditSinkRead` walks up from a reference to its member and names the first sink met.
A reference that is only the receiver of a member access is not a sink.
Parentheses and a null-forgiving `!` between the two do not change that.
The walk stops at the enclosing member declaration.
The target of `?.` whose tail requests is gatekeeping, since its presence decides the request.
A hot argument is a replaying sink.
So is a value written into a record copy or the initialiser of a logic or relay construction.
The condition of an if, ternary or switch whose branch requests is a gatekeeping sink.
So is the condition of a while, do or for loop whose body requests.
So is a `when` clause on a switch arm or a catch that requests.
So is the left side of `&&`, `||` or `??` whose right side requests.
An if or ternary condition that only tests for null or for a type is not a gatekeeping sink.

`TAuditGatekeepingCheck` is true when a branch requests, or jumps out while the scope it leaves requests from its condition on.
A `return` or `throw` leaves the member.
A `continue` leaves its loop body, and a `break` leaves its loop or its switch section.
A request later in that loop or section is skipped by the jump, so it counts.
A request after the loop runs whatever the branch decides, so it does not count.
A request in a later case is not skipped by the `break`, so it does not count.
A request before the branch runs whatever the branch decides, so it is not gatekeeping.

`TAuditPresenceCheck` is true when a condition, past parentheses and negation, only tests for null or matches a type.
Such a condition decides nothing the engine holds and is never gatekeeping.
An `&&` or `||` chain counts only when every operand is such a test.

`TAuditRequestCheck` is true when a node contains a logic call, a logic construction or a call to a relay.

`TAuditHotCheck` is true for any argument of a logic call.
It is also true for an argument at a hot position of its callee.
A named argument is matched to its parameter by name.

`TAuditZeroingCheck` is true when an argument of a call or construction stands at a Zeroing hot position.
Any argument of a logic call is hot, as in `TAuditHotCheck`.
Otherwise the position must sit in `TAuditZeroingNames`, the only position map this check reads.
The callee is the relay or logic symbol, else the plain method or constructor called.
So a driver constructor or helper outside the relay set can still be hot.
An indexer argument or a `this` or `base` initialiser is never hot.

`TAuditZeroingScan` reports every literal zero or default that a driver sends toward a gate or compares with a gate-bound member.
From the literal it climbs through parentheses, casts, either arm of `?:` and the right side of `??`.
A literal that then stands at a Zeroing hot position is a hit named after the callee.
A constructor callee is named by its type, and the reason reads `passes 0 to new T`.
A literal compared with a hot-read member or a logic member is a hit named after that member.
The comparisons are `==`, `!=`, `<`, `>`, `<=`, `>=`, `is 0` and `is not 0`.
A count or length compared with zero reaches no gate, so it is no hit.
Each literal yields at most one hit, on the literal's own line.

`TAuditZeroCheck` is true for the literal `0` or `0L`, the `default` literal and a `default(T)` expression.
A `null` literal is not one, since it names absence in the type itself.

`TAuditComparedRead` gives the field or property on the other side of a zero comparison, when that member counts.
Parentheses, casts, `?.` and a null-forgiving `!` are looked through to the member read.
It counts when read inside a Zeroing hot argument or when it is a logic member.
Otherwise it is null, so a framework count or a local compared with zero is no hit.

`TAuditCallRead` gives the logic or relay symbol a call or construction invokes, else null.
Invoking a delegate member that is a relay returns that member.
Raising an event that implements a relay interface event returns that event.

`TAuditDelegateRead` gives the delegate member a call invokes, directly, through `Invoke` or through `?.Invoke`.

`TAuditInterfaceCheck` is true when an event implements an interface event in the relay set.
A subscription through `INotifyPropertyChanged` thus reaches every type that raises it.

`TAuditNameofCheck` is true for a `nameof` expression, which yields a name and reads no value.
A method a driver itself names `nameof` resolves to a symbol and is not one.

## Stamp

Commit: `90a13c9590ba`
- `TAssayControl.cs` `7665198e484be7fd`
- `TAssayRelay.cs` `4d5cbd221728c8e7`
- `TAssaySink.cs` `921bd5e11c87862f`
- `TAssayTruth.cs` `74d379ab6ea10564`
- `TAssayZeroing.cs` `1421a9346abb4fa4`
- `TAuditTruthRelay.cs` `55ad73318110b1ce`
- `TAuditTruthSink.cs` `9c23cfa573bae31a`
