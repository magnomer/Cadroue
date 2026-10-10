# TAssayBorder.cs
Hash: `003e8ab936d475e4`

## `public sealed class TAssayBorder`

Assays of the border walker's linger scan: small hand-written sources with an expected hit or no hit.
Each assay binds through `TAuditBinder.TAuditAssayRun` and runs the walker's real entry.
Assays check the walker, not the tree, so they gate no tree result and touch no ceiling.
A failing assay exposes a walker bug, never a source to fix.

## `private const string TAssayPortPath = "src/Cadroue.ShellEngine/Assay/LAssayPort.cs";`

The virtual path of the stand-in port, under ShellEngine so its event sits below Conduct.

## `private const string TAssayHolderPath = "src/Cadroue.Conduct/Assay/CAssayHolder.cs";`

The virtual path of the subscribing type, under Conduct so the linger scan reads it.

## `private const string TAssayPortText = """`

A ShellEngine interface declaring one event, the guarded source every holder subscribes to.

## `public void AuditBorder_UnremovedHandler_ReportsLingering()`

A method handler added with no removal anywhere in its type is one hit at the `+=` line.

## `public void AuditBorder_DetachedHandler_AllowsSubscription()`

A detach method removing the same event and handler pairs the subscription, so no hit.

## `public void AuditBorder_OtherHandlerRemoval_ReportsLingering()`

A removal of a different handler leaves the subscription unpaired, so it stays one hit.

## `public void AuditBorder_LambdaHandler_ReportsLingering()`

A lambda handler is one hit even beside a removal, since no `-=` can name it.

## `public void AuditBorder_ConductEvent_AllowsSubscription()`

An event declared on another Conduct type is not guarded, so its unpaired subscription gives no hit.

## `private static IReadOnlyList<TAuditHit> TAssayBorderRun(string holder)`

Binds the port and the given holder and returns every hit of `TAuditBorderLinger.TAuditLingerScan`.

## `private static string TAssayHolderFormat(string handler, string detach)`

A Conduct holder keeps the port in a field.
It adds the handler in one method and writes the detach line in another.
The `+=` always sits on line 17, so every positive assay expects that line.
