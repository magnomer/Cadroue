# LFixPlan.cs

## `public static LWorkFix LFixPersistentRead(LSidecarFixRecord lFixRecord, bool lFixSalvagePersistent)`

The session snapshot only ever holds persistent steps, so every step it carries is persistent.
The salvage-persistent flag is carried alongside it in the tab layout.

## `public static LWorkFix? LFixPlanRead(string lFixSourcePath, Func<string, LSidecarFixRecord?> lSidecarRead)`

A per-file plan is never persistent: persistence is a session concept, never stored in the sidecar.
