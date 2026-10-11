# TViewfinderSelection.cs
Hash: `0e7532e9b575ce60`

## `public sealed class TViewfinderSelection`

Checks the viewfinder deportment without any control.
It covers the press and drag decisions, the section bands over the working range and the label placement.

## `public void Press_SelectsTheSectionUnderTheCursor_AndDragsThePlayhead()`

A press both selects the section under the point and starts a playhead drag.
Dragging into another section moves the playhead but keeps the selection.
A drag notice brackets the gesture, and a flow with no media refuses the press.

## `public void Bands_ClampToTheRange_AndFeedThePopupOffset()`

Bands are cut to the visible working range, so a narrowed range moves their edges.
The popup offset of a missing section is zero.

## `public void Label_FitsBadgeThenName_AndPlacesBoth()`

The number badge is fitted first and the name takes the room left.
A band too narrow for the badge yields no label.
