# QEditProcessing.cs
Hash: `0c704bf29c7b935b`

## `public static class QEditProcessing`

Holds the Edit tab's processing rows as static data.
The Veneer reads them once to build the processing panel in this order.

## `public static readonly IReadOnlyList<QProcessingRow> QEditRows`

Lists each Edit step with its icon path and label key.
The order is the order the panel shows, so tests pin the first and last row.
