# LMessengerFunnel.cs
Hash: `e9f76fc0e49ecb30`

## `public static partial class LMessenger`

Routes Funnel tab files to other tabs by filename rule, without creating work.

## `public static int LMessengerFunnelDescribe(`

Only rules whose target tab is set are eligible to match.
A route that loops back through automatic funnels is refused and recorded through LTraceEngine.
When the relay-empty preference is on, delivered files are drained from the source tab.
Ends with an LSeal run, so merge nodes see the files just delivered.
Returns the number of files delivered.
