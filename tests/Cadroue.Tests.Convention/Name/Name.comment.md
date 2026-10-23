# Name

This folder holds the name audit with its walker, filter and setting.
It also holds the specimen and violation records.
The generated base, verb and exemption registries live in `Registry`.

## `TAuditName`

Runs the name audit over every source the naming scope enumerates.
The scope comes from `TAuditNameSetting`, so the file set follows the naming configuration alone.
An empty source list fails, since the audit would otherwise pass vacuously.
Every violation is listed with its file, line, kind, name, and reason.
Violations sort by repo-relative path, then line, then name, as AuditNames.ps1 sorts its hits.
Every registry exemption must still spare a name in the sources, so a stale row is removed from the registry.
The turf fact counts every type whose prefix lies outside the turf of its project.
A turf is a project folder, and `TAuditPrefixTurfs` lists the prefixes it allows.
A public type in a sealed turf is left to its own fact, so it never hides under the ceiling.
The count must equal `TAuditPrefixCeiling`, so it fails above the ceiling and when the ceiling is stale.
The ceiling only falls as types move to their turf or take its prefix.
No out-of-turf type in a sealed turf is public on itself and on every containing type.
A sealed turf exposes only its own prefix, so a stray public type leaks its turf.
The sealed hit is hard, with no ceiling, and the script reports it as the `Sealed turf` gate.
A parsed public class under a sealed turf's path, carrying another turf's prefix, gives one sealed hit.
The same class declared internal gives none, so it stays in the turf count.
The turf scan binds nothing, so a specimen can feed it a hand-written source.
A name without a prefix is left to the name fact, so no type counts twice.
A source outside every turf gives nothing.
A hit is sealed when its turf is sealed and the syntax marks it public up the whole chain.
Accessibility is read from syntax, as the script reads it, since neither side binds the turf scan.
A sealed hit adds `and public in a sealed turf` to the line the script prints too.
Each hit prints as `path:line [Kind] Name - reason`, the same line the script prints.

## `TAuditNameFilter`

What says a specimen lies outside the audit.
A name fixed from outside, by a framework contract, an override, an extern or an attribute, is skipped.
A declaration marked as generated code is skipped.
A name a source generator emits is built by the walker and audited at its declaration's line.
The prefix read here decides which prefix a name carries, if any.
The walker asks these questions and audits only what passes.
Every type part is indexed before any name is judged.
The key is namespace, enclosing types, name and arity, as AuditNames.ps1 keys it.
Each type key is visited once, so a cycle in the base lists cannot loop.
A contract declared by a source base type counts too, as it does in AuditNames.ps1.

## `TAuditNameSetting`

Every naming-audit setting lives here and nowhere else, hand-written and tracked.
No script writes this file, and no registered name may appear in it.
Bases, verbs, and exemptions reach the tests only through the generated registries in `Registry`.
The tests read no script configuration, so no json or ps1 can change what they check.
They do read the untracked sources Git lists, since an unstaged file is still source.
The prefix ceiling is the number of types whose prefix lies outside their turf.
It only falls, and the script holds the same number as `naming.prefixCeiling`.
Prefixes are listed longest first, so a prefix is read before any shorter prefix it starts with.
Each project folder lists the prefixes its types may carry.
The script holds the same map as `naming.prefixTurfs`.
The sealed turfs are those whose out-of-turf types may not be public.
A public type there is a hard hit kept out of the turf count, so the ceiling stays unchanged.
The script holds the same list as `naming.sealedTurfs`.

## `TAuditNameWalker`

Both name audits, this one and the script, investigate the same names and share no code.
Both must carry the same generation, here as `TAuditNameSetting.TAuditGeneration`.
A disagreement between them is a defect in one, for a person to read.
A generation names the set of checks the audit applies, so two projects on one generation compare directly.
A check added, removed, or changed in what it reports is a new generation.
Wording, plumbing, refactoring and edits to this file leave the generation alone.
This walker and `TAuditNameFilter` are identical in every project at the current generation.
It checks missing prefix, component count, base registration, and verb ending.
It fails a name that does not split into prefix and PascalCase components, as the script does.
It fails a type, delegate or event name that ends in a verb, as the script does.
It exempts a framework contract name only when a part of the type declares the interface.
It exempts descriptive test methods under an all-or-nothing test-prefix consistency gate.
It audits a generated name at the declaration it is built from.
It clears a name only through a scoped row of the exempt table.
Sources reach it through a `TAuditScope`, which leaves out the tooling's own files.
The turf count lives in `TAuditName`, which this walker leaves alone.
Every name the sources declare is read, the markup's included.
The code trees are parsed first, so the partial index sees every part before any name is judged.

## `TAuditRegistry`

The registered bases, verbs, and exemptions the name audit checks against.
They load from the generated registry files, so the tests read no document and no embedded resource.
The bases come from one file per role, joined into one set.
Those files hold names only, never a setting.
Regenerate them before the tests, since a stale registry audits against stale names.
An exemption row grants its name only inside the files it names.
The same word stays a violation everywhere else.
`*` grants a mechanism that is universal by spelling, such as a template part.
File names in an exemption compare case-insensitively, since the file system does.

## `TAuditViolation`

Gives the reason a name breaks the naming rules, or null when it passes.
A test method is judged only when some test method in the sources carries a prefix.
Such a method must carry `TAuditNameSetting.TAuditTestPrefix`, and a different prefix or none is a reason.
Any other name loses its leading underscores first.
A name without a registered prefix is missing its prefix.
A prefix with nothing after it leaves no base.
The rest splits at underscores into segments, and an empty segment is a reason.
Each segment must split wholly into PascalCase components by `TAuditNameSetting.TAuditComponentPattern`.
The first component must be a registered base.
A method kind must end in a registered verb.
A data or type kind must not end in one.
More components than `TAuditNameSetting.TAuditComponentLimit` is a reason.
The base, verb and component-limit reasons are joined with a comma, so one name reports all of them.

## `TSpecimen`

One name the walker found, with its file, line, spelling and the kind of declaration it came from.

## `TViolation`

One name that broke a rule, with its file, line, spelling, kind and the reason in words.

## Inline notes

### TAuditNameFilter.cs: `internal static bool TAuditContractCheck(SyntaxNode node, string name)`

A member may be a contract member of a framework interface the enclosing type declares.
Such a name is externally fixed by that interface, the same reason an explicit interface implementation is exempt.
The rule is applied here to the implicit form.
For a partial type the interface may be declared in another part in another file.
The walker indexes every part first, so every part's interfaces are read.
A contract name on a type that declares no such interface anywhere is audited like any other.

### TAuditNameWalker.cs: `if (registry.TAuditExemptValidate(candidate.TSpecimenName, candidate.TSpecimenPath))`

A row of the registry's exempt table grants its name only inside the files that row lists.
The same word stays a violation in every other file.
A mechanism that is universal by spelling is registered there with the `*` scope.
Only the user adds a row, and working a report never grants one.

### TAuditNameWalker.cs: `if (node is MethodDeclarationSyntax commandMethod)`

A source generator emits a name the source never spells, so the audit builds it here.
A method carrying a command attribute yields the property the generator emits.
Its name is the method's with the async suffix dropped and the command suffix added.
The generated name is a data member and is governed as one.

### TAuditNameWalker.cs: `bool anyTestPrefixed = candidates.Any(candidate =>`

The test suite either names every test method descriptively without a prefix.
Otherwise it commits to the test prefix on every one.
A single test method with the test prefix flips the whole suite into the prefix-required mode.
While no test method uses the test prefix the whole suite is descriptive and exempt.
A test method then carries a free-form scenario description, never an object base or verb.

### TAuditViolation.cs: `return "missing required prefix";`

A codebase-owned name that survived the external, generated and framework-contract filters but carries no prefix is a violation.
It is not something to skip.

### TAuditViolation.cs: `return "an underscore leaves an empty component";`

A malformed name is a violation, never a silent pass.
So a prefix followed by an underscore alone, or a trailing underscore, cannot slip through.

## Stamp

Commit: `90a13c9590ba`
- `TAuditName.cs` `f792b24acaff5b40`
- `TAuditNameFilter.cs` `94d5e4f62b0c181b`
- `TAuditNameSetting.cs` `0a7c49402958360c`
- `TAuditNameWalker.cs` `23089b13ebf9ab23`
- `TAuditRegistry.cs` `428c2b1c1830ca58`
- `TAuditViolation.cs` `545a2ac20b7bb981`
- `TSpecimen.cs` `0b6da9e6a472965e`
- `TViolation.cs` `9173679d3acf4860`
