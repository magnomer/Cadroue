# QEditTool.cs
Hash: `7067878c98ce15ba`

## `public sealed class QEditTool`

Gates the crop tool on the Edit tab from the inspector arm and the list lock.
It holds only references, so the crop overlay keeps the tool and lock state.

## `public void LEditToolHandle(bool lArmed)`

Passes the inspector's crop tool arm straight to the crop overlay.

## `public void LEditLockHandle(bool lLocked)`

Locks or unlocks the crop overlay when the selected file's lock changes.
A lock cancels any neutral pick, and an unlock rearms the tool only when the inspector armed it.
