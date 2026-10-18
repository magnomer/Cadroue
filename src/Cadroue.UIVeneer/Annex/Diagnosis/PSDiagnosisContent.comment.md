# PSDiagnosisContent.cs
Hash: `2bfdfc99a449ed81`

## `private UIElement PSDiagnosisBannerBuild()`

The banner shows the overall verdict and the Recheck button, which restarts the probe through QSDiagnosis.

## `private UIElement PSDiagnosisProgramBuild()`

Builds the program version and location group at the top of the window.
Its values arrive later through the result apply member.

## `private UIElement PSDiagnosisFeatureBuild(string pLabelKey, string[] pFilters)`

Each feature row remembers its filter names with its badge.
The result apply then asks QSDiagnosis for the state of exactly those filters.

## `private static void PSDiagnosisBadgeApply(Border pBadge, TextBlock pText, QSDiagnosisState pState)`

Any state other than ready or missing draws the neutral checking badge.

## `private void PSDiagnosisResultApply()`

The probe notice may arrive off the UI thread, so the member re-enters itself through the dispatcher.
Every badge and the summary are redrawn from QSDiagnosis on each notice, so no partial state lingers.

## `private void PSDiagnosisSummaryApply(QSDiagnosisMood pMood, int pMissing)`

Warning and missing moods share the count text and differ only in the dot colour.
