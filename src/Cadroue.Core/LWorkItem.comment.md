# LWorkItem.cs

## `public bool LWorkSourceMeasured { get; set; }`

False until the low-priority source measurement has been attempted.
The worklist shows "Measuring" rather than "Unknown" while it is still false.
So a not-yet-measured source is never mistaken for one that could not be measured.
