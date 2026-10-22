# QMergeTab.cs

## Inline notes

### `QMergeTab.cs: qMergeDocket.LDocketChanged += QMergeDocketHandle`

Auto grouping reads the unlocked entries, so lock state is part of group eligibility.
The docket change event fires on add, remove, claim, release and delivered add, so every change of eligibility regroups.
A subscription to adds alone would miss a claim or a release.
Test `DocketChange_AutoOn_GroupsSeries_AutoOffLeavesGroupsAlone` pins the regroup and the auto-off case.
Test `EligibleRead_SkipsLockedGroups_PathsReadSkipsLockedFiles` pins that locked files stay out.

### `QMergeTab.cs: QMergeClose`

Close unsubscribes the change and the removed events, so a closed tab stays out of the docket.
