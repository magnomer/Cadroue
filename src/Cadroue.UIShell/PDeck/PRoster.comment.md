# PRoster.cs

## `private void PRosterElapsedTick(object? pSender, EventArgs pArguments)`

A running job has no finish time yet, so its elapsed figure is measured against the clock.
The shown item's source figures land later, from the low-priority background measurement.
This one-second tick refreshes the detail while any job runs.
Then elapsed and the batch's summed spent/speed advance in real time.
It also refreshes while the shown item or batch is still awaiting measurement.
Then "Measuring" turns into values as soon as the result lands.
