# Truth

This folder holds the driver audit with its test, setting, ledger, walker, field and reference model.
It also holds the local, sequence and tampering halves.

## `TAuditTruth`

The driver rules say a driver gathers input, calls a Conduct gate and shows the verdict.
It never builds, checks or decides what a gate owns, and it reaches the engine only through Conduct.
The rules hold every driver ring alike.
The walk runs once and every fact reads the same hits.
While `TAuditTruthEnforced` is false every fact passes and only prints its count.
Enforced, a kind fails in any file whose count stands above its ledger ceiling.
The ledger keys each ceiling by file, so each medium's lag is its own.
A new file starts at zero.
`TAssayTruth` and its `TAssay` siblings assay the walker on hand-written sources.
Assays check the walker, not the tree, so they gate no tree result and no ledger ceiling.
The report is written once, on the first kind check that runs.
Kinds whose hits name a line rather than a field are left out of the field table.
A driver value that reaches a request, as an argument or as the condition around one, is gatekeeping.
A request is a call to a Conduct gate, to the engine or to a driver relay of either.
Gatekeeping counts every condition, including `if`, `?:`, `switch`, loops, `when` clauses, catch filters, `?.`, `&&`, `||` and `??`.
Gatekeeping through a getter property counts as a read of the field it returns.
An engine answer or a dialog answer deciding a request is gatekeeping too, since the gate owns that decision.
Zeroing is a literal `0`, `0L` or `default` sent toward a gate, where it means none, new, silence or append.
That meaning belongs to the gate, so a driver that spells it out decides for the gate.
A literal compared with a member that reaches a gate counts too.
Zeroing reads a wider hot map than Replaying, with constructors and stored parameters added.
A driver field written from an engine result in one place and from the driver in another is contesting.
Two writers make a copy that drifts from what the engine holds.
A driver field or settable property holding a type from below Conduct is a hit.
So is a field named as a state, typed `object`, or caching the answer to a request.
So is a driver type deriving from or implementing a type from below Conduct.
A Conduct type is held freely, and a Conduct port is implemented freely.
A driver line that assigns a logic member or reorders a collection mutates logic.
So does one that overrides a logic member in a record copy.
Misfiring is a control or console input deciding a request.
A clock driving a request is misfiring, and so is an observer that never reads its bulletin.
It is also a member sending a second request, since one user action is one gate.
Moonlighting is a driver line that compares, computes or queries over a value from below Conduct.
Reshaping a Conduct verdict is the driver's work and is not moonlighting.
A driver identifier holding a character outside ASCII is a homoglyph.
A driver line is laundering when it computes over a carried engine value.
Computing over the input of a control or the console is laundering too.
A driver line handing its surface a type from below the cut, Conduct's included, is spoonfeeding.
A surface binding reads the members of what it is fed, so spoonfeeding is where the cut is crossed.
Mismatching is a Conduct member one driver reaches and another never does.
It is also a Conduct port a driver does not implement.
Drivers call the same gate for the same action, so an asymmetry is medium work or a leak.
It only informs while a driver ring it waits on holds no source, as `TAuditTruthInformative` sets.
An informative kind waiting on an empty driver prints its count and fails nothing.
A kind outside the informative list always gates.
A listed kind gates once every folder it waits on holds a walked source.
Puppeteering is a member of a control type that requests logic, directly, through a relay or through a lambda.
A control type is a driver type whose name starts with its ring's prefix in `TAuditControlPrefix`.
A control shows what its driver hands it, so the request belongs in the driver.
Reading a Conduct record property is no request and stays legal.
Every ledger ceiling equals its count, so a ceiling left above the count is stale.
The lowered ledger is written under `temp/audit` to copy over the tracked one.
Every tracked driver source must reach the walkers.
A file the binder dropped does not pass unseen.
The walk fails when no tracked file matches the include patterns.
Every driver source is enumerated with Git, and the driver, moonlighting and laundering walkers run once.
Paths are made repo-relative.
The report opens with the counts, then the fields by hit count, then every hit by kind.

## `TAuditTruthField`

One audited field, with the symbols that mean it, its label, its type and where it is declared.
A positional parameter carries both its parameter symbol and the property it generates.
The record also lists which members of a class are audited fields.
`TAuditTruthWalker` judges each one it yields.
The audited members are the mutable fields, the settable properties and the primary-constructor parameters of a type.
A `readonly` or `const` field is a fixture while nothing fills it and it holds no engine type.
A write is an assignment, a slot or a fill verb on any part of the class.
`TAuditValueCheck` names a value type, meaning a bool, char, number, string, enum, or the nullable form of one.
A value-typed `readonly` field set at construction is identity, not state, so it is not audited.
A `readonly` value field ignores every write made directly in a constructor of the declaring type.
A write from a lambda, a local function or another member still counts, since it can change the value later.
A positional record parameter of a value type is identity unless the record is copied with `with` or written.
`TAuditCopyResolve` collects every record type that any `with` expression copies, and its record base types.
`TAuditTruthWalker` runs it once over the roots before the fields are listed.
A `static` field is shared and its writes are read in every file.
A settable property and a positional parameter are always shared.
A field initialised to `null!` is audited like any other, since its type says what it holds.
A field typed as a handle grips a gate rather than holding a value, and is skipped.
A getter property that reads the field is followed as the field, so gatekeeping cannot hide behind it.

## `TAuditTruthLedger`

The driver audit's ceilings, by kind and then by repo-relative file.
Each ceiling is the count of that kind in that file, and every kind may only fall.
A kind with no rows stands at zero in every file.
A file missing from a kind has a ceiling of zero.
A kind that only informs holds no rows.
Once the driver ring it waits on holds a source, every hit of it fails.
`TAuditRatchet` holds every ceiling against the committed copy, and a new generation baselines it afresh.
When a count falls, the truth ledger fact writes the lowered ledger under `temp/audit` to copy here.

## TAuditTruthLocal

The local half of the truth walker, covering what a member may not do with an engine answer it holds.
A Conduct type's member is an answer too, since a gate's verdict may not decide the next request.
Reference lookups and line numbers come from `TAuditTruthReference`.
A field written before and again after a request in one member is gatekeeping the shell runs itself.
Each write must sit in a block that also holds the request, so both run around it.
Writes in branches the request never passes through do not toggle.
Only the write after the request is read past a `catch` or `finally`.
A write in a `catch` or `finally` runs after the request its `try` makes.
A `catch` before a later request usually leaves the member, so it does not open a hold.
A call to a method writing the field counts as a write, so a hold split into relays still toggles.
The writers are the class methods that write the field and request nothing themselves.
A method that requests is a relay and is audited on its own.
A fill of a collection is a refresh rather than a hold and does not count.
The hit lands on the last write and names the relay it went through.
A driver type deriving from or implementing an engine type holds logic by inheritance.
Implementing a Conduct port is what a driver is for, so it is no hit.
The scope is every member of a class and of the types nested in it, field declarations included.
A lambda in a field initializer is scanned like a method body, so it cannot hide an answer.
Locals initialised from an engine answer are followed to the sinks a field would be.
A member read off the answer is followed as the answer itself, so an id lifted from a draft counts.
An answer carried into the next request, or deciding one, is a value the engine should have kept.
A hit is reported once per line, local and kind, and named by member and local.
A local is answered by an initialiser, an assignment, a deconstruction, a loop variable or a pattern.
An `out` argument and a lambda parameter of a request count too.
An initialiser is an answer when it requests logic, calls a relay, or reads a logic member.
A name inside `nameof` reads nothing and does not count.

## `TAuditTruthReference`

Finds the references to a symbol in the walked roots and says what each reference does.
The truth walker's parts, its field, contest and origin helpers all share these answers.
The index is static state, so it is only rebuilt under `TAuditTruthWalker`'s gate.
It groups every identifier of the walked roots by the symbol it resolves to.
A field's references are looked up there rather than found by scanning every file again.
A name inside `nameof` reads nothing, so it is left out of the index.
A rebuild drops every entry of the last walk.
Taint is one hop only, so a local built from a tainted local is not followed.
The reference node is the access when the identifier is a member of `this` or a named owner, else itself.
A reference is written when it is assigned, incremented or passed by out or ref.
It is also written when a slot of it is assigned or a fill verb is called on it.
The value is the right side of a plain assignment, or the last argument of a fill.
A fill without an argument empties the field and is sorted as a clear, not a writer.
A name inside `nameof` does not count in a name check either.

## TAuditTruthSequence

The sequence half of the truth walker.
One driver member may not dictate a run of requests.
One user action is one gate, and the order of several is a gate's decision.
Line numbers come from `TAuditTruthReference`.
The send names are the members that send a request directly, read before the walk.
A sender is a method or property that reaches a send root.
It may instead call a Conduct method or build a request record.
The sender resolve takes one hop only, so a member that merely shows or fills is not a sender.
Send sites inside a scope are read in source order.
A site is a root call, a Conduct call, or a request record built outside a send.
A call to a sender counts too when not direct.
A site nested inside another send counts once.
A gate call is an instance method declared on a Conduct type.
Every member whose sends are not all exclusive is reported once.
It is reported on the first send that clashes with an earlier one.
Two sends are exclusive when their nearest shared ancestor is a branch and they sit in different arms.
An earlier send inside an `if` that jumps out is exclusive with what follows it.
An `if` jumps when it has no `else` and its body always leaves by `return`, `throw`, `break` or `continue`.
Only then can a send inside the body and a send after the `if` never both run.
A jump counts only when plain blocks alone lie between it and the `if`.
A jump under a nested `if`, loop, switch, `try`, `using`, lambda or local function may not run.
So a `break` or `continue` that counts always targets a loop or switch enclosing the `if`.
A `throw` expression such as `x ?? throw` is not a jump statement and does not count.

## `TAuditTruthSetting`

Hand-written and tracked.
The driver-audit scope, the binder inputs and the handle list live here.
The per-file ceilings live in the ledger named by `TAuditLedgerFile`.
No script writes this file.
The generation numbers the revision of the driver-audit settings this file holds.
The enforced switch false makes every driver fact a warning that passes.
True fails a fact on any file whose count of a kind stands above its ledger ceiling.
The report path carries the version.
A field or property whose name ends in the state suffix is a state the engine should own.
The bulletin type is the engine's notice type.
A handler or lambda taking one and never reading it is a deaf observer, in any medium.
The configuration is the build whose output and generated files the binder reads.
The reference root is the project whose build output carries every package any source references.
The Conduct root holds the types of the gate layer.
A driver may hold, implement and reshape them, since the gate layer's types stop at the driver.
The Capsule include patterns name sources that count as shell.
A call into one answers a shell value, so Contesting does not search its arguments.
Every reader of the shell include joins this list to it, so each path is written once.
A type declared under the shell or Capsule patterns is shell.
A type declared under the Conduct root is a gate type.
A type declared under any other source is engine.
The truth include patterns select the driver sources the driver walks audit, every driver ring alike.
Informative kinds only inform, each with the driver folders it waits on.
Such a kind gates nothing while a listed folder holds no tracked source.
Once every listed folder holds one, the kind fails like any other.
The control prefix of each driver ring is keyed by the ring's folder.
A driver type whose name starts with its ring's prefix is a control type.
A control shows what it is handed and may not request logic, so puppeteering names any request inside one.
`AuditUI.json` holds the same rows as `truth.controlPrefixes`, copied by hand.
The framework packs are the shared frameworks referenced beside the test runtime, so framework types resolve.
A type deriving from a control base is a GUI control, whose state may not decide a request.
The order verbs are collection calls that change an order, which only the engine may decide.
The fill verbs write into a collection in place, so a `readonly` field filled through one is not a fixture.
Every request record carries the request prefix.
The send roots are the calls a request finally goes through.
A member that reaches one, calls a Conduct method or builds a request is a sender.
The clock types fire on time rather than on a user act.
The console input calls carry what the user typed, the console's control input.
Each is written as the full name of its declaring type and member.
The dialog types give an answer that confirms a request.
A confirm belongs in the gate behind a port, so a dialog answer deciding a request is gatekeeping.
The delay members are the waits that turn a loop into a clock.
The input base is the base type of every control that takes user input.
The laundering walk colours an input member only on a receiver deriving from it.
`AuditUI.json` holds the same name as `truth.inputBase`, copied by hand.
The input members of a GUI control carry what the user typed or chose.
`AuditUI.json` holds the same list as `truth.inputMembers`, copied by hand.
The focus members tell whether the user is at a GUI control.
A guard on one of them only asks whose event it is, so the misfiring scan exempts it.
The handles are driver types a driver field may hold as a handle rather than as a value.
A handle is called on and passed on, so the field is skipped before any rule reads it.
A Conduct type is a handle by rule and is never listed.
A type from below Conduct that is not listed is no handle, so holding one is a duplicating hit.
A type is matched as the binder shows it, with any nullable mark dropped.
The moonlighting verbs are query methods that, applied to a value from below Conduct, are moonlighting.

## TAuditTruthTampering

The tampering half of the truth walker, covering what a driver line may not do to logic without a request.
It also holds the spoonfeeding rule and the mismatching rule.
The mismatching rule reads every driver file at once.
Line numbers come from `TAuditTruthReference`.
A Conduct port is reported at its own declaration line instead.

`TAuditTamperingScan` reports every plain assignment to a logic member outside an object initializer.
An event subscription is a compound assignment and is not reported.
A tuple assignment over indexed slots is a swap, and a swap reorders a collection.
A `with` copy that sets a logic member rewrites logic the engine handed over.
A slot assignment on a store of rows overwrites what the engine ordered.
A slot assignment on a local the member built empty fills it and is not reported.
Every order verb on a store of rows is also reported, since the engine alone owns an order.
The spoonfeeding scan runs first on the same file.

`TAuditSpoonfeedingScan` reports every value of a type from below the cut.
The value is one that a driver hands a control or a surface type.
An assignment, an object initializer, a call argument and a construction argument are each a handover.
A surface binding reads the members of what it is fed.
So typed bindings are held here rather than in markup.
A lambda is wiring rather than data and is left alone.

`TAuditSurfaceRead` is true when the receiver, or any object along its member chain, is a control or a surface type.

`TAuditMismatchingScan` reports every Conduct member one driver reaches and another never does, once per file and member.
It also reports every Conduct interface that a driver declares no implementation of, once per driver.
The drivers stand on one Conduct, so an asymmetry is medium work or a leak, and the ledger holds it.

`TAuditFreshCheck` is true when the collection is a local declared empty, by an empty collection expression or a bare construction.
Such a local holds nothing the engine handed over.

`TAuditStoreCheck` is true when the collection's type holds logic, shell rows or untyped objects.
An expression the compiler cannot type is taken as a store, since the walk cannot clear it.

## `TAuditTruthWalker`

Follows each driver field to where its value goes, by symbol.
A settable property and a positional record parameter are followed the same way.
Logic is what `TAuditBinderSide` says lies below the cut, Conduct's gates and the engine alike.
A request is a call into logic or into a driver relay of one.
A field holding a Conduct type is a handle, while a field holding an engine type is duplicating.
Partial classes are joined by their type symbol.
This part also holds the handle and alias rules that decide which fields the walker follows.
The field, local and sequence scans read nested types and field initializers with their outer class.
`TAuditTruthField` lists the audited fields of a class.
`TAuditTruthReference` finds and sorts the references to them.
`TAuditTruthContest` reports a field with both an engine and a shell writer.

`TAuditGate` guards the walker's state, which lives in statics.
The truth facts and the walker's assays run on parallel threads.
The identifier index in `TAuditTruthReference` is such state too.
Both public entry points take the gate, so one walk finishes before another resets the state.

`TAuditRoots` holds the walked roots, so a write to a shared field from another class is still seen.

`TAuditRun` scans every driver file for tampering, spoonfeeding, misfiring and zeroing, then checks mismatching across the drivers.
It then names every control member in the relay set as puppeteering and checks every type.

`TAuditHandleCheck` is true for a Conduct type, which a driver is meant to hold.
It is also true for a listed handle, shown minimally and with any nullable mark dropped.

`TAuditAliasRead` gives the field and every getter-only property of its class that reads it without requesting.
A getter reading such a property is followed too, to a fixed point.
Gatekeeping through one of them is gatekeeping on the field itself.
A parameter the field is passed to by `ref`, `out` or `in` is the field itself inside that method.
Such a parameter passed on again is followed too.

`TAuditPuppeteeringScan` names every control member that stands in `TAuditPuppetNames`, one hit per member.
A member that reaches logic only by raising an event is no hit, since the handler requests.
A delegate field or property a driver fills still counts, since the control invokes it.
A method, property, field or event counts, while a local function or local stays with its member.
The hit lands on the member's first declaration in a walked driver source.
A control shows what it is handed, so a request inside one is driver work done in the wrong type.
The reader set is never consulted, so reading a Conduct record property is no hit.
Hits are sorted by path, line and name, so the order holds whatever the set's order.

`TAuditControlCheck` is true when the type name starts with the control prefix of the driver ring holding the path.
`TAuditTruthSetting.TAuditControlPrefix` keys each prefix by its ring folder.

`TAuditReaderRead` gives the driver members that read logic or request, for the laundering scan.

`TAuditPartRead` keeps the walked roots, groups the outermost types, builds the identifier index and reads the relays and senders.

`TAuditFieldCheck` treats a field holding an engine type as duplicating before any reference is read.
A field typed `object` is duplicating too, since an untyped slot hides what it holds.
A field whose name ends in the state suffix is duplicating too, since only the engine resolves a state.
A `??=` whose right side requests caches the answer and is duplicating.
It visits every reference to the field and its aliases, outside its class only for shared fields or non-private symbols.
A use of a ref parameter the field was passed to counts as inside, wherever its method lies.
A write is sorted by `TAuditOriginWalker.TAuditWriterResolve`.
A `ref` write into a plain helper is sorted by the argument the helper assigns.
A fill without an argument empties the field and is sorted as a clear.
The sorted writes go to `TAuditTruthContest.TAuditContestCheck` in reference order.
A read is checked once per member scope.

`TAuditScopeCheck` checks the field and the locals it taints inside one member for a sink.
A hit is reported once per line and kind.
A name inside `nameof` is skipped.
A hit through a ref parameter names that parameter, as a hit through a local names the local.

## Stamp

Commit: `90a13c9590ba`
- `TAuditTruth.cs` `0158f5aa35dfba0a`
- `TAuditTruthField.cs` `bda475280d6b7bcb`
- `TAuditTruthLedger.json` `c50bb25b609b47e9`
- `TAuditTruthLocal.cs` `4baaaa83b0678024`
- `TAuditTruthReference.cs` `3c96dcfa5aa99002`
- `TAuditTruthSequence.cs` `a5a77f14c0b84d98`
- `TAuditTruthSetting.cs` `650669334584d064`
- `TAuditTruthTampering.cs` `78e63b421c0ee57e`
- `TAuditTruthWalker.cs` `6f9d273d1926e234`
