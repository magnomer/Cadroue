# TAssayContestingCarrier.cs
Hash: `565860d3d28c6128`

## `public sealed class TAssayContestingCarrier`

Assays of the contesting hit when the copied value travels through an object.
The carrier is a positional record, an auto property or a constructor parameter.
They run through `TAuditTruthWalker.TAuditRun` with the helpers of `TAssayTruth`.
Each pair judges one rule, as a hit beside a no-hit.
Each rule was switched off once, and its no-hit failed.
A failing assay exposes a walker bug, never a source to fix.

## `public void AuditTruth_PlainRecord_ReportsContesting()`

A positional record property built from a literal is plain, so the copy contests.
The record reaches its reader through `Array.ForEach`, a method group, so the reader's parameter stays unseen.

## `public void AuditTruth_EngineRecord_AllowsEngineCopy()`

The same property built from an engine read is engine through its `new` argument.

## `public void AuditTruth_PlainProperty_ReportsContesting()`

An auto property assigned a literal is plain.

## `public void AuditTruth_EngineProperty_AllowsEngineCopy()`

The same property assigned an engine read is engine through the assignment.

## `public void AuditTruth_PlainConstructor_ReportsContesting()`

A constructor parameter takes its `new` arguments, here a literal.
The object reaches its reader through `Array.ForEach`, so only the constructor rule decides.

## `public void AuditTruth_EngineConstructor_AllowsEngineCopy()`

The same parameter fed an engine read is engine.
