# Script parameter grammar

This file is the single lexicon of parameter names across every script in `scripts/`.

## Rule

A parameter name carries exactly one meaning.
A script may lack a parameter, but a script that has it uses that meaning.
One meaning carries exactly one name, so no two names share a meaning.
An alias is a name too and follows the same rule.
PowerShell common parameters already own their meanings, so no script parameter repeats one.

## Adding a parameter

Look the meaning up in the lexicon below first.
When a row matches the meaning, reuse its name unchanged.
When no row matches, pick a name no row holds and add the row in the same change.
A name whose meaning must change is renamed everywhere in the same change.

## Common parameters

| Name | Meaning | Scripts |
|---|---|---|
| `-Verbose` | Print the detail the script otherwise leaves out. | Check |
| `-WhatIf` | Show every write without performing it. | Clean, Version |
| `-Confirm` | Ask before every write, and `-Confirm:$false` skips every prompt. | Clean, Version |

## Lexicon

| Name | Meaning | Scripts |
|---|---|---|
| `-Help`, `-?` | Print the help and exit without doing anything else. | all |
| `-Root` | Project root to work on, defaulting to the parent of `scripts/`. | AuditEncoding, AuditFake, AuditNames, AuditNamesNew, AuditObject, AuditPlatform, AuditStructure, SyncNames |
| `-ConfigPath` | JSON configuration file, defaulting to the script's own file beside it. | Audit, AuditComments, AuditEncoding, AuditLines, AuditUI |
| `-Configuration` | Build configuration, `Debug` or `Release`. | AuditUI, Test |
| `-SourceRoots` | Source roots to scan, overriding `sources.roots`. | AuditComments, AuditLines |
| `-Extensions` | File extensions to scan, overriding `sources.extensions`. | AuditLines |
| `-Exclude` | Git pathspecs left out of every measurement. | VEstimate |
| `-Segments` | Path segments under a root that form one folder row. | AuditComments, AuditLines |
| `-MaxWords` | Word limit of one comment line, overriding `rules.maxWords`. | AuditComments |
| `-LimitThreshold` | Last line count that passes, overriding `thresholds.limit`. | AuditLines |
| `-WarningThreshold` | Last line count below the warning band, overriding `thresholds.warning`. | AuditLines |
| `-Commits` | How many recent commits feed the statistics. | AuditLines, VEstimate |
| `-Top` | How many rows a console table shows. | AuditFake, AuditObject, AuditStructure, StatsSyntax, StatsWords, VEstimate |
| `-OutputPath` | File the Markdown output is written to. | AuditComments, AuditLines, AuditUI, TraceValue |
| `-ReportDirectory` | Folder the Markdown report is written to under its versioned name. | AuditFake, AuditObject, AuditPlatform, AuditStructure |
| `-Keep` | Keep the existing output instead of clearing it first. | Audit |
| `-Open` | Open the report once the run finishes. | AuditComments, AuditEncoding, AuditFake, AuditLines, AuditNames, AuditObject, AuditPlatform, AuditStructure, AuditUI, Check |
| `-NoOpen` | Write the page without opening it. | Audit, AuditComments, AuditEncoding, AuditFake, AuditLines, AuditLinesHistory, AuditNames, AuditObject, AuditObjectHistory, AuditPlatform, AuditStructure, AuditUI, StatsSyntax, StatsWords |
| `-NoPause` | Never stop at a console page. | AuditComments, AuditFake, AuditLines, AuditNames, AuditObject, AuditPlatform, AuditStructure, StatsSyntax, StatsWords, VEstimate |
| `-Name` | Symbol names the walk starts from. | Test, TraceSymbol |
| `-Depth` | Maximum hops a walk takes, where 0 walks until nothing new turns up. | TraceSymbol, TraceValue |
| `-Entry` | Method that takes the injected value. | TraceValue |
| `-Value` | Value injected into the entry. | TraceValue |
| `-Parameter` | Parameter of the entry that takes the value. | TraceValue |
| `-Json` | Write the result as JSON instead of the report. | TraceSymbol |
| `-Version` | The `major.minor.revision` version the script acts on, or `stable` where accepted. | Build, Run, Stop, TimeMachine, Version |
| `-Command` | Version operation to perform. | Version |
| `-Rebuild` | Discard the existing result and produce it again from scratch. | AuditLinesHistory, AuditObjectHistory, Build, GitDownload, Run, TimeMachine, VEstimate |
| `-All`, `-a` | Take every item instead of the default subset. | Stop, Test |
| `-Force` | Kill a process at once instead of asking its window to close. | Stop |
| `-TimeoutSeconds` | Seconds to wait for a window to close before the kill. | Stop |
| `-NoBuild` | Skip the build and use the existing build output. | Check, Test |
| `-NoRestore` | Skip the package restore. | Test |
| `-NoTest` | Skip the test run. | Check |
| `-NoAudit` | Skip the audits. | Check |
| `-NoLaunch` | Build and install without starting the application. | TimeMachine |
| `-Grep` | Patterns whose every hit in the sources counts as a failure. | Check |
| `-Filter` | VSTest filter expression that selects the tests. | Test |
| `-Project` | Single test project to run. | Test |
| `-Convention`, `-c` | Run the convention test project only. | Test |
| `-Main`, `-m` | Run every test project except the convention project. | Test |
| `-List` | List the matching tests without running them. | Test |
| `-Repeat` | Number of runs, stopping at the first failure. | Test |
| `-Verbosity` | Logging level passed to `dotnet test`. | Test |
| `-AdditionalArguments` | Arguments passed unchanged to `dotnet test`. | Test |
| `-Runtime` | Runtime identifier for the .NET publish. | Debug |
| `-TempOnly` | Remove only stray WPF temporary-project files. | Clean |
| `-IdeCache` | Also reset the VS Code C# Dev Kit workspace cache. | Clean |
| `-FingerprintOnly` | Print the source fingerprint without writing an archive. | Zip |
| `-Repository` | GitHub repository in `owner/name` form. | GitDownload |
| `-SnapshotDirectory` | Folder that receives the version folders and their archives. | GitDownload |
| `-Threshold` | Exclusive version threshold saved to `snapshot.json`, where 0 takes every version. | GitDownload |
| `-Destination` | Folder the dispatchers are written to. | Install |
| `-File` | Text file of name tokens to check. | AuditNamesNew |
| `-Token` | Name tokens to check. | AuditNamesNew |
| `-Round` | Granularity of the suggested version step. | VEstimate |
| `-MovedWeight` | Weight of a moved line. | VEstimate |
| `-ReflowWeight` | Weight of a reflowed line. | VEstimate |
| `-DeletedWeight` | Weight of a novel deleted line. | VEstimate |
| `-PathWeights` | Weight of each path class. | VEstimate |
| `-Calibrate` | Search the line weights for the least spread and print the best pair. | VEstimate |

## Internal parameters

These names exist for one script calling another and are never typed by hand.

| Name | Meaning | Scripts |
|---|---|---|
| `-CurrentDestination` | Layout key that receives a current-source build. | TimeMachine |
| `-WorkRoot` | Layout key whose temp subfolder holds the intermediate files. | TimeMachine |
| `-MirrorCurrentSnapshot` | Also copy a current-source publish into its snapshot folder. | TimeMachine |
| `-Invoker` | Name of the delegating script, written into the record. | TimeMachine |
| `-SnapshotStagingDirectory` | Staging folder used while installing a current snapshot. | Zip |
