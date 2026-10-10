# TAuditBorderLinger.cs
Hash: `496fe4eddfee1b26`

## `internal static class TAuditBorderLinger`

Finds Conduct subscriptions to events of deeper rings that are never removed.
Such a subscription keeps the Conduct type alive as long as the deeper type lives.

## `public static IReadOnlyList<TAuditHit> TAuditLingerScan()`

Reads every `+=` and `-=` in Conduct whose left side binds to an event.
A `+=` is guarded when the event's declaring type sits in a ring below Conduct.
The ring comes from the declaring type's source path, never its name prefix, so L-named Conduct events stay unguarded.
A guarded `+=` is a `Lingering` hit unless its type removes the same event with the same handler.
The removal may sit in any partial part and any method, and its receiver need not match.
A lambda or anonymous method handler is always a hit, since no `-=` can name it.
A type subscribing to its own event is skipped.
The pair runs from Conduct to the event's ring, and one hit is kept per line and name.
