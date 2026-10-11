# TCropAnchor.cs
Hash: `e1ff07bfd89b7a95`

## `public sealed class TCropAnchor`

Checks the crop overlay deportment grip, anchor and state defaults without any control.
It also holds one check of the loupe playing and ended state.

## `public void Grip_DrivesFromEdge_AnchorsOpposite(int edgeX, int edgeY, int drive, int anchorX, int anchorY)`

Dragging a grip resizes from the grabbed edge.
The opposite edge or corner stays fixed as the anchor.
A grip drag is never a whole box move.

## `public void Body_ClearsEdges_MovesWholeBox()`

Grabbing the box body clears the grip so the drag moves the whole box.

## `public void Draw_ResetsAnchor_KeepsEdges()`

Drawing a new box resets the drive and anchor.
The edge values from the last grip are left as they were.

## `public void Ratio_InvalidBecomesFree()`

A ratio with a zero side is not a ratio.
Both sides drop to zero, which means free.

## `public void Lock_DefaultsOpen_ActiveDefaultsOn()`

A new crop overlay is active, unlocked and not persistent.

## `public void Loupe_PlayingClearsEnded_ResumeNeedsBoth()`

Playing clears the ended flag.
Resuming after a pause needs the loupe to have played and not ended.
The playing notice fires on each flip.
