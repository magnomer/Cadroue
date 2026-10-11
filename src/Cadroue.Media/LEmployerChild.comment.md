# LEmployerChild.cs
Hash: `33cbc4e1752fac3c`

## `public static class LEmployerChild`

The employer side holds every live queue child by work id, so the engine never names an OS process.
It suspends and resumes those children at OS level for the runner's pause and play.
One lock covers the table and the held set, so a registration and a suspend never both suspend one child.

## `private static readonly HashSet<Guid> lEmployerHeld = new();`

A held work id is one the runner wants paused, whether or not its child has started yet.
A child registered under a held id is suspended at once, so a paused runner never gets a running child.
The hold lasts until the runner resumes or forgets the id, so later stages of a paused job start suspended.

## `internal static void LEmployerChildAdd(Guid lEmployerWorkId, Process lEmployerProcess)`

Registers a child right after it starts, and suspends it when its work id is held.
A failed suspend leaves the child running rather than failing its run.

## `internal static void LEmployerChildRemove(Guid lEmployerWorkId)`

Forgets the child when its run ends, but keeps any hold, since only the runner releases a hold.

## `public static bool LEmployerChildSuspend(Guid lEmployerWorkId)`

Holds the work id and suspends its live child, and reports false only when that child refuses.
An id already held reports true without a second suspend, so one resume always undoes it.
An id with no live child is held and reports true, so its coming child starts suspended.

## `public static bool LEmployerChildResume(Guid lEmployerWorkId)`

Releases the hold and resumes the live child, and reports false only when that child refuses.
A refused resume keeps the hold, so the next play retries the same child.
An id that is not held reports true, since nothing of it was suspended.

## `public static bool LEmployerChildCheck(Guid lEmployerWorkId)`

Reports whether a registered child under the work id is still running.
The runner uses it to tell a live child from one that is only held or already finished.

## `private static bool LEmployerProcessCheck(Process lEmployerProcess)`

A child disposed before its removal counts as exited instead of throwing under the lock.

## `private static extern int NtSuspendProcess(IntPtr lRunnerProcessHandle);`

The ntdll calls suspend and resume every thread of the child in one step.
They live on the employer side, so the engine keeps no platform call.

## `private static bool LEmployerProcessSuspend(Process lEmployerProcess)`

Any failure to reach the handle reports false, so a refused child never throws into the runner's pause.
