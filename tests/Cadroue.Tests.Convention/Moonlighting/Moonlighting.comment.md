# Moonlighting

This folder holds the rules that keep a driver from deciding, with the assays that check them.
The rules are misfiring, gatekeeping, moonlighting and laundering.

## `TAssayCarrier`

Assays of the misfiring scan on control reads that reach a condition indirectly.
A read may travel through a local, a parameter or a local function's parameter.
It may also sit in a type pattern on a value typed `object`, as a `sender` is.
They use the helpers of `TAssayTruth`, and body line k is driver line 9 + k.
A local, a parameter or a local function parameter fed a control read carries it into the `if`.
The same three fed a value that reads no control carry nothing.
A type pattern that narrows `object` to a control and tests its text reads the control.
The same pattern testing only the type of `DataContext` is pairing.
The same holds for a switch case on `object`.
A bare type name parses as a constant pattern, yet it is only a type test.
A constant value decides, and an `or`, a `not` or parentheses over type names stay a type test.

## `TAssayHearing`

Assays of the focus exemption in the misfiring scan.
A focus guard that only drops an event the user did not cause is hearing, not misfiring.
A focus read that chooses between paths, or any other control state, stays a hit.
Body line k is driver line 9 + k.
An early `return` when the box lacks keyboard focus only drops the event.
The same `return` on another state, or when the box has focus, still decides.
The pattern form tests focus `true` beside a type test, with the request inside.
The same pattern testing focus `false` runs the request only for an event the user did not cause.
A focus read joined by `&&` to a part that reads no control is still a focus guard.
A focus read joined by `||`, followed by an `else`, or followed by a later request chooses between paths.
Those forms stay misfiring.

## `TAssayLaundering`

Assays of the laundering walk, run through `TAuditLaunderingWalker.TAuditRun`.
The readers come from `TAuditTruthWalker.TAuditReaderRead`, as in the tracked audit.
The driver carries two reader members that read the gate, so their results are logic values.
The gate is this file's own source in the conduct layer, with a static property and an enum beside `CAssayRead`.
A logic bool compared with another value is a laundering decision.
A comparison with `true` or `false` is not, and fails when the literal comparison is no longer skipped.
A static property of a conduct-layer type is a logic source.
An enum member of a conduct-layer type is not.
A chain that joins a presence test with a plain operand decides an if.
A chain of presence tests joined by `&&` decides nothing, and fails when chains are no longer split into operands.
A logic value read inside an interpolation is still its source.
A logic local named inside `nameof` is not read.
The `Text` of a `Run` or a `TextBlock` is no control input.
A `TextBlock` does not derive from the input base, so the user cannot change it.
The `Text` of a `TextBox` is control input.
The `IsChecked` of a `ToggleButton` is control input, which fails when the base walk checks only the direct base type.
An input member on a `dynamic` receiver keeps its colour.
The assay compilation rejects errors, so `dynamic` stands in for a receiver type the model cannot resolve.

## `TAssayMisfiring`

Assays of the misfiring scan on control reads in `switch`, `if` and ternary forms.
The control is a local `TextBox`, since the walker knows controls by their framework base.
Body line k is driver line 9 + k.
The local is on line 10 and the next statement on line 11.
A `when` guard that reads a control and picks a request decides it.
The hit sits on the switch section.
A constant label on a switch governed by a control read picks the request by the control's value.
A type-only label, property pattern or guard on the control's `DataContext` is pairing, not a control decision.
A property pattern that holds a value tests the control's state, in a switch, an `if` or a ternary.
The hit twin `GuardNestedConstant` fails when the guard rule is removed, which the no-hit twin alone cannot show.
A switch-expression arm with a constant on a control read picks the request.
The hit sits on the arm, and the discard arm beside it adds none.
A lone discard arm or a `default` label tests no value, so it decides nothing.

## `TAssayRelational`

Assays of the misfiring scan on relational and default forms, run through the helpers of `TAssayTruth`.
Body line k is driver line 9 + k.
A relational label, pattern or arm on a control read tests its value.
Those fail when a relational pattern counts as pairing.
The same label on a value that reads no control makes no hit.
A guard or ternary comparing a control's value with a constant decides.
Comparing with `null` is a presence test.
The control decides whether the `default` section or discard arm runs, so the switch decides its request.
The hit then sits on the `case "a"` section or `"a"` arm, whose label reads the control.
That fails when only the section's own request counts.

## `TAuditLaunderingWalker`

Follows an engine value into a local or a driver member and reports the line that computes over it.
The moonlighting rule sees only a line that names logic, so a value copied into a local escaped it.
A control's input and the console's input are followed the same way, since a driver passes them raw.
A taint carries the colour `logic value` when it came from logic.
It carries `control input` when it came from an input member of a control or a console read.
The driver members that read logic or request come from the truth walker.
They are held in an async-local value, like the assay compilation in `TAuditBinder`.
So an assay walking beside the tracked walk never swaps the tracked readers for its own.
Outside a run it is null.
The caller's readers are held for the walk and cleared when it returns, even on a failure.
Every driver file is walked and each member scanned on its own, since a taint lives inside one member.
A sink whose expression already names logic is the moonlighting rule's and is skipped here.
A comparison with a `true` or `false` literal only coerces a value, so it is no operator sink.
A hit is reported once per line and reason, with the first line of the node as its name.
A parameter of a type from below the conduct layer is tainted from the start.
An initialiser, an assignment to a local, a loop variable and a pattern designation carry their source's colour.
A local built from a tainted local is followed, so the hop count is not limited to one.
A tainted local, an engine symbol, a reader member, a control input member and a console read each count.
A control input member counts only when its receiver is accepted as a control the user can change.
A static conduct-layer field or property is a logic value too, as a driver method returning it is.
An enum member is not, since comparing to one only dispatches on a verdict.
A name inside `nameof` reads no value and is skipped.
The receiver's type must derive from `TAuditTruthSetting.TAuditInputBase`, resolved through the semantic model.
A receiver of any other type holds only what the driver wrote.
An error type, a `dynamic` receiver or a missing type keeps the colour, so no unresolved read escapes.
A namespace receiver, as in `System.Text`, reads no value and is false.
A null check, a type test and a bare verdict decide nothing about a value.
An `&&` or `||` chain counts as presence only when every operand is a null check or type test.
A condition that is only a logic call or a reader call has its answer audited elsewhere.

## `TAuditMoonlightingWalker`

Finds the driver lines that compute over engine data, since a driver must not decide the data.
A value of a conduct-layer type is the driver's to reshape, so only types from below the conduct layer count.
Every driver source is scanned for homoglyphs and for moonlighting.
One hit is kept per line and reason.
A logic value as an operand, query source, cast, `typeof`, attribute or condition is a hit.
Null checks, coalescing and the logical connectives are not moonlighting.
A condition that is a bare verdict is not moonlighting either, since asking the engine a question is allowed.
An equality test naming a setter's own `value` is not moonlighting.
That comparison only skips a change that changes nothing, so it computes over no logic.

## `TAuditTruthControl`

The control reading of the truth walker, which says whether a condition reads what the user can change.
`TAuditControlRead` names the first control read in a condition, else null.
A member access on a control that is not itself logic is a read.
A local or parameter that carries a control read is a read too.
A call to a console input member is a read.
Last, a type pattern that names a control and tests its state is a read.
So `sender is TextBox { Text: "a" }` reads a control although `sender` is typed `object`.
The carried names are cached per member declaration, so a carrier is found once per member.
A declaration or assignment whose value reads a control carries it.
An argument that reads a control carries it into the local function's parameter.
The nodes are read in order, so a carrier copied from a carrier carries too.
A designation in a pattern never carries, since a type test pairs a row and reads no state.
`TAuditShapeCheck` is true for a property pattern on a control type that tests more than type.
`TAuditPairCheck` is true for a pattern of type tests alone, nested property patterns included.
Such a pattern only pairs a row to its record.
A bare type name parses as a constant pattern, so a constant naming a type is a type test.
Parentheses, `not` and a binary pattern keep a pattern of type tests alone.
`TAuditFocusCheck` is true for an `&&` chain whose every control read asks only that the control has focus.
A focus member comes from `TAuditTruthSetting.TAuditFocusMembers`.
In a control's type pattern, a focus member tested `true` is a focus read.
Other parts of such a pattern must be type tests alone, so the row is only paired.
Parts that read no control may test anything, since they are not the medium's state.
A negated focus read, an `||` or a carried focus value is no focus guard.

## TAuditTruthMisfiring

The misfiring half of the truth walker, covering what the driver's medium may not decide.
A control's state, console input, a clock and a deaf handler count as misfiring.
None of them is a user act or an engine fact.
It also holds the gatekeeping rules for answers a driver branches on.
Line numbers and name matching come from `TAuditTruthReference`.
Whether a condition reads a control comes from `TAuditTruthControl`.
Each hit is reported once per line, kind and name.
An `if` or ternary whose condition reads a control or console input and whose branch requests lets the medium decide.
A switch case or arm deciding on a control read is the same misfiring, when any section or arm requests.
Every form exempts the same reads, a presence test or an `is` test of type tests alone.
So the verdict never depends on the syntax the author picked.
An `if` that only hears whose event it is decides nothing, so a focus guard is no hit.
An `if` deciding a request on a dialog answer is gatekeeping.
The gate asks the user through a port instead.
An `if`, ternary or switch deciding a request on an engine answer is gatekeeping, since the gate owns it.
A clock drives a request through an event, a callback it is built with or a loop that waits.
A method or lambda taking a bulletin it never reads is misfiring, since it acts on a signal it ignored.
The lambda rule reads the parameter's type, so it holds for every medium and every observer helper.
A condition asks the engine when it reads logic, calls logic or calls a driver member that reads logic.
A verdict wrapped in a driver method is still the engine's answer.
An `if` without `else` that only drops an event the user did not cause is not misfiring.
Dropping such an event picks no gate and decides no data.
An early exit qualifies when its body is a bare `return` and its condition negates a focus guard.
Any other `if` qualifies when its condition is a focus guard and no request follows it in the member.
So the false path drops the event, and no other arm calls a gate.
An `else` always keeps the hit, since focus would then choose between two paths.
A `when` guard that reads a control decides, unless it is a presence test or type tests alone.
Otherwise a control read in the governing expression decides when the label tests its value.
A null label, a discard and a label of type tests alone test no value.
A label whose pattern tests a control's state through a type pattern names the governing expression.
Picking a gate by the row's type pairs the row to its record, which a driver may do.
A constant or any other value test in the pattern decides.
A clock is an expression, or the object a creation builds, of one of the clock types.
A loop becomes a clock when it waits on a delay member or on a clock.
A handler, a callback or a loop drives a request when it requests inline or names a relay.
A deaf handler has a bulletin parameter the body never mentions.

## Stamp

Commit: `90a13c9590ba`
- `TAssayCarrier.cs` `44fe74f05ccf40bc`
- `TAssayHearing.cs` `7ca66e7cd6ca4cc3`
- `TAssayLaundering.cs` `9f90ce311d0c887c`
- `TAssayMisfiring.cs` `4b1e417aa0f723e9`
- `TAssayRelational.cs` `d83dcafcc2218a98`
- `TAuditLaunderingWalker.cs` `a1fdc8f98a04be4b`
- `TAuditMoonlightingWalker.cs` `0e07bcc8450a5218`
- `TAuditTruthControl.cs` `995beaacfbabe3f5`
- `TAuditTruthMisfiring.cs` `c75914b5fbdcd9c3`
