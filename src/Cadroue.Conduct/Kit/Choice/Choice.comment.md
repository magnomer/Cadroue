# Choice

This folder holds the semantic answer of a choice list.
Presentation only localizes and projects it.
A gate answers the rows and the selected row, and the surface never decides which row is chosen.

## CChoice.cs

A row carries the token a selection maps to, the wording key and the chosen flag.
The token is null for the custom row.
The basis key is set only on a custom row that shows the preset it started from.

## CChoicePlan.cs

The plan is a list of semantic rows plus the selected row.
Build takes the index of the first chosen row, so no chosen row reads as minus one.
The rows are copied into a read-only list, so a later change to the caller's list cannot change the plan.

## CChoicePreset.cs

One row is made per preset, in the order the tool hands them.
The row of the matched preset is chosen.
A custom row comes last and is chosen exactly when no preset row is.
When no preset matches and a token was last chosen, the custom row shows that token's wording as its basis.
So an edited value still names the preset it drifted from.
Test `Choice_DriftKeepsToken_AndSelectionRestoresPreset` pins the drift and the restore.

## Stamp

Commit: `b84e5458de50`
- `CChoice.cs` `85d1adcfafea23b1`
- `CChoicePlan.cs` `bebd259d3da38cf0`
- `CChoicePreset.cs` `295165da1956a478`
