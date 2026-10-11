# TLineageEntries.cs
Hash: `db35e2a0e46a6f6c`

## `public sealed class TLineageEntries`

Checks how the lineage deportment groups schedule items into entries and formats their text without any control.
The Schedule collection keeps it from running beside tests that share the schedule.

## `public void Read_GroupsAThreeStageBatchIntoOneEntry_WithTitleStepsAndRatios()`

A chain of derived outputs in one batch forms one entry with the first source as subject.
Each stage ratio is its output size against the origin size.
A missing origin size reads as a dash.

## `public void Read_SeparatesBatches_AndKeepsFirstSeenOrder()`

Items of different batches never share an entry.
Entries follow the order in which each batch is first seen.
A blank path reads as no path.
