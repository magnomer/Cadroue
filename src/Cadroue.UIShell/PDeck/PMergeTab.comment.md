# PMergeTab.cs

## Inline notes

### `lDocket.LDocketChange += PMergeDocketHandle`

Auto grouping reads `PListUnlockedRead`, so lock state is part of group eligibility.
`LDocketChange` fires on add, remove, claim and release, which is every event that changes eligibility.
Subscribing to `PListItemsAdd` alone left a locked file visible in its group, so the whole group was skipped.
It also left an unlocked file missing until an unrelated add.
