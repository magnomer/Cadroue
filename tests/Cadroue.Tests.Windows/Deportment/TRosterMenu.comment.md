# TRosterMenu.cs
Hash: `b9ef07599c1a52fe`

## `public sealed class TRosterMenu`

Checks the roster's context menu plan and its item actions without any control.
The Schedule collection keeps it from running beside tests that share the schedule.

## `public void Pending_OffersCancel_AndCancelRunsOverTheSelection()`

A pending job offers cancel.
Running the item cancels every selected job and not only the clicked one.

## `public void Cancelled_OffersRestart_ForTheClickedItemWhenItIsNotSelected()`

A click outside the selection acts on the clicked job alone.
Restart returns that job to pending and leaves the selected one cancelled.

## `public void Done_WithoutAnOutputFileOrStrip_HasNoMenu()`

A finished job offers actions only when its output exists or a strip can receive it.
An unknown job id has no menu.
