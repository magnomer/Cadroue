# LEditPlan.cs

## `public bool LEditPlanActive`

A plan is active once any processing item has its Apply toggle on, even with no settings behind it.
Crop Apply on with no rectangle, or a video step at its default value, still queues an Edit job.
That job re-encodes the source through the export preset, the same way Convert does.
Only Apply toggles decide activity, stored geometry or values never do on their own.
Add List reads this from the sidecar, so an active plan must also be written.

## `public bool LEditPlanEmpty`

Empty means nothing worth writing to the sidecar: no active plan, no crop geometry, no fixed ratio.
