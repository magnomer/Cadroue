# PRosterSummary.cs

## Inline notes

### `long pOutputTotal = 0`

Only completed outputs are totalled, since a progressing job's growing file must not be accounted.
So the comparison stays source vs finished output and grows as jobs land.
