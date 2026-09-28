# TAuditNameWalker.cs

## Generation

AUDITNAMES GENERATION 15.

A generation is not a revision count.
It names functionality, not edits.
Editing this file is never on its own a reason to raise it.
Raise it only when the audited outcome changes.
A generation names the set of checks the audit applies.
Two projects on the same generation audit the same things.
Their reports compare directly, whatever else differs between the files.
A check added, removed, or changed in what it reports is a new generation.
Wording, plumbing, and refactoring leave the generation alone.

This project is one of two implementations that investigate the same names.
Neither shares code with the other, and that is the design.
Both must be at the same generation, so both carry the number.
A disagreement between them is a defect in one, for a person to read.

Generation 8 holds every naming value in the hand-written `TAuditNameSetting.cs`.
The registered names reach this project only through the generated `TAuditNameRegistry.cs`.
Every other file in this project is identical in every project at this generation.
Both files are committed, so this project builds on its own.

Generation 8 checks missing prefix, component count, base registration, and verb ending.
It exempts descriptive test methods under an all-or-nothing test-prefix consistency gate.
It audits a generated name at the declaration it is built from.
It clears a name only through a scoped row of the exempt block.

Generation 9 changes nothing the name audit reports.
The number rises with the custody and strict audits, which share it.
It does not audit the tooling's own files, and sources reach it through a `TAuditScope`.
What says a specimen lies outside the audit lives in `TAuditNameFilter.cs`.

Generation 11 changes nothing the name audit reports.
The number rises with the truth audit, which gains four deportment-field checks.

Generation 12 exempts a framework contract name only when a part of the type declares the interface.
The number also rises with every audit rebaselined against the structure report.
It fails a name that does not split into prefix and PascalCase components, as the script does.
It fails a type, delegate or event name that ends in a verb, as the script does.

Generations 13 and 14 change nothing the name audit reports.
The number rises with the UI audit, whose hook rule gains slots and a literal rule.

Generation 15 adds the ring count in `TAuditName`, which this walker leaves alone.
The UI audit gains the pack, scaffold and contract kinds in the same generation.

## `public static List<TSpecimen> TAuditSpecimenRead(IEnumerable<string> sourcePaths)`

Every name the sources declare, the markup's included.
The code trees are parsed first, so the partial index sees every part before any name is judged.

## Inline notes

### `if (registry.TAuditExemptValidate(candidate.TSpecimenName, candidate.TSpecimenPath))`

A row of the `AUDIT:EXEMPT` block grants its name only inside the files that row lists.
The same word stays a violation in every other file.
A control template part such as `PART_Track` is registered there with the `*` scope.
Only the user adds a row, and working a report never grants one.

### `if (node is MethodDeclarationSyntax commandMethod)`

A source generator emits a name the source never spells, so the audit builds it here.
A `[RelayCommand]` method yields the `{Name}Command` property that actually exists.
The generated name is a data member and is governed as one.

### `bool anyTestPrefixed = candidates.Any(candidate =>`

The test suite either names every test method descriptively without a prefix.
Otherwise it commits to the T prefix on every one.
A single T-prefixed test method flips the whole suite into the prefix-required mode.

### `if (!anyTestPrefixed)`

A test method carries a free-form scenario description, never an object base or verb.
While no test method uses the T prefix the whole suite is descriptive and exempt.
Once any test method adopts T, every test method must carry it.

### `return "missing required prefix";`

A codebase-owned name that survived the external/generated/framework-contract filters but carries no prefix is a violation, not something to skip.

### `return "no base after the prefix";`

A malformed name is a violation, never a silent pass, so `L_foo` or `LDraft_` cannot slip through.
