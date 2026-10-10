# LJobSmart.cs

## Inline notes

### `LRunner.LRunnerRecord(`

Inside `LJobSmartRun`, this is the only whole-interval encode path.
Smart-mode jobs rejected earlier by eligibility checks use the ordinary stage path instead.
Both Whole and Invalid plans take this path, so it is not restricted to a missing copyable middle.
Fresh media probes for the boundary decision receive the runner's priority override, keeping planning under its execution configuration.
