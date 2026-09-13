# LMediaLoad.cs

## `public sealed class LMediaLoad : IDisposable`

Owns the backend lifecycle of the current media source.
A load is one operation: validation, probing, cancellation, ordering, and committing current identity happen here.
