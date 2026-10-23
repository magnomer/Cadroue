# PStrip.cs

## `public sealed class PStrip`

The strip registers WPF tab builders and places built surfaces into the deportment deck.
`QStrip` retains tab, selection and deck decisions.
This class supplies concrete surface composition.

## `public PStrip(QStrip lStrip, Func<QExport, PExport> pExportBuild)`

The shell injects a factory that turns each workspace's `QExport` into a prepared `PExport`.
Each Export-bearing builder requests one surface and passes it directly to its tab constructor.
The strip does not choose Export resources, routes or presentation implementations.
Funnel and Worklist builders do not request Export surfaces.

## `public PTabSurface? PStripSelected`

The selected surface is resolved through the deck index.

## `public QStripTab PStripAdd(QStripTab lStripTab, LSceneTabEntry? lPreferenceTabLayout = null)`

The catalog builds the tab surface before the workspace attaches its layout reader.
The tab then attaches strip-specific behavior, and the workspace frame receives the prepared surface plus Export-toggle and close routes.
Only after the deck index can resolve the surface does `QStripAdd` publish the tab.
Failure during construction or frame wiring prevents that final publication.
No fallback tab is created.

## PStrip.xaml

The resource dictionary owns the strip accent brush only.
