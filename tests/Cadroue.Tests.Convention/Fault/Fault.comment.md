# Fault

This folder holds the fault audit counting swallowed catch clauses, with its setting, walker and assay.

## `TAssayFault`

Assays of the fault walker, run through `TAuditFaultWalker.TAuditFaultScan`.
`TAuditFault` runs the same scan over the tree.
The assayed source sits in a ring the setting lists, so the walker reads it.
The clause always opens on line 30, so an expected hit names that line.
`LAssayRecord` is an interface, so a call through it is a recording, never a way out.
`LAssayKeep` stores its argument in a field, so its parameter is carried.
Recording the fault through an interface keeps it from the user, so the clause is a hit.
Turning the fault into null drops it, so the clause is a hit.
A rethrow hands the fault out, even after recording it.
A fault assigned to an `out` parameter reaches the caller.
Raising an event tells a listener, even when the fault itself stays behind.
A cancellation is exempt by shape, even with an empty block.
A fault passed to a ring helper that stores it in a field is carried out through the helper.
A check expects exactly the one hit, or none.

## `TAuditFault`

Counts the catch clauses of the listed rings that swallow a fault, so the count can only fall.
A swallowed fault is only recorded or turned into null and never reaches the user.
Each ring has its own ceiling, and an exempt row counts against none.
The same count is audited by `AuditFault.ps1` from its own configuration, and neither reads the other.
The hits are bound once and shared by every fact.
No ring holds more swallowing catch clauses than its ceiling.
Each ring above its ceiling lists every hit it holds.
A ceiling above its count is stale and fails, so a fixed catch is locked in.
An exempt row that matches no hit fails and must be deleted.
An exempt row names the file and enclosing method of the hit.

## `TAuditFaultSetting`

Hand-written and tracked, with no script writing it.
`AuditFault.ps1` reads its own AuditFault.json and never writes this file.
The rings are the projects whose catch clauses are walked, each under `src/`.
The ceiling is the `Swallowing` hit count each ring may reach, exempt rows left out.
A count above its ceiling fails the fact, and a ceiling above the count is stale and fails too.
Lower a ceiling when a catch hands its fault out or is removed.
Never raise one to admit a new one.
Exempt rows are `path:EnclosingMethod` whose catch clauses swallow by design and count against no ceiling.
The ceilings and the exempt rows live in the project's settings files.
A row that matches no hit fails, so no row outlives its catch.

## `TAuditFaultWalker`

Finds the catch clauses of the listed rings that keep a fault from ever reaching the user.
A clause hands its fault out by throwing, by carrying its variable out, or by invoking a delegate or event.
Recording the fault through an interface or into another ring is not handing it out.
Every other clause is a `Swallowing` hit, unless it catches a cancellation.
`AuditFault.ps1` applies the same rule from its own helper, and neither reads the other.
A catch of `OperationCanceledException`, or of a type derived from it, is exempt by shape.
A cancelled operation is an answer, not a fault.
The scan settles the carried parameters first.
So a fault passed to a ring helper is judged by what the helper does.
It returns one row per swallowing clause, in path and line order, exempt rows included.
Judging a clause reads nothing but its arguments, so the same clause always gets the same verdict.
A throw, a delegate call or an event raise in the block hands the fault out.
Nested lambdas and local functions are skipped, since they may never run.
The hit name is the enclosing member, so a catch in a lambda belongs to the method holding it.
A constructor is named after its type and an accessor after its property.
A catch with no type shows as `Exception`.
Carrying the variable out of its own locals means a way out.
  - An assignment to a field, a property, or an `out` or `ref` parameter is one.
  - A `return`, an argument to a delegate call, or an argument to a carried ring parameter is one too.
  - Assigning a local or passing the variable to any other call is not.
The carried ring parameters are settled to a fixed point.
A parameter passed on to another carried parameter is carried, however long the chain.
An expression body of a method returning a value carries what it returns.
A partial method keys its parameters by the defining part, which a call binds to.
An expression holds the variable when it is the variable or a cast or `as` of it.
Either arm of a conditional counts too.
So does a creation, tuple, collection, array or `with` holding it as an argument or initializer.
A call result never holds the variable, since the callee decides what it returns.

## Stamp

Commit: `90a13c9590ba`
- `TAssayFault.cs` `caf373b1d9ff54db`
- `TAuditFault.cs` `b6f2ce6aeb4ab086`
- `TAuditFaultSetting.cs` `fd8c678a9331bf53`
- `TAuditFaultWalker.cs` `a9c101c8ddd998bc`
