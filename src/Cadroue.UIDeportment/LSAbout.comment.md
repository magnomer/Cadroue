# LSAbout.cs
Hash: `5b47e61173cb456f`

## `public event Action<string, string>? LSAboutErrorShown;`

Hands a failed open to the Veneer as the resource and the error detail.
The trace record is already written by `LUsher`, so the Veneer only shows the warning.

## `public bool LSAboutTapChange(DateTime lNow)`

Counts taps on the program mark toward the hidden developer switch.
A gap longer than the tap window restarts the count at one.
The tenth tap flips the developer preference and resets the count.
True tells the Veneer the switch just flipped.

## `private static string LSAboutNoticePath => LUsher.LUsherProgramResolve(LSAboutNoticeName);`

Names the third-party notice shipped beside the executable, at the path `LUsher` builds.

## `public void LSAboutNoticeOpen() =>`

Opens the third-party notice with its system handler.
A failure is recorded under the `Notice` label and raised through `LSAboutErrorShown`, never thrown.

## `public void LSAboutLinkOpen(string lUrl) => LSAboutErrorRaise(lUrl, LUsher.LUsherLinkOpen(lUrl, "Link"));`

Opens a credit or project link in the system browser.
A failure is recorded under the `Link` label and raised the same way.

## `private void LSAboutErrorRaise(string lResource, string? lDetail)`

Raises `LSAboutErrorShown` only when the open returned an error.
Showing the warning is the About window's decision, so it stays here and not in `LUsher`.
