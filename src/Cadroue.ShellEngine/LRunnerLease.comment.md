# LRunnerLease.cs
Hash: `a7cb7fc306cc9692`

## `public static Action<Guid> LRunnerAddSeam { get; set; } = _ => { };`

Registers a new runner with the liveness registry the host owns.
Pairs with the remove seam, so a runner is registered once and cleared once.
Unattached, it does nothing, so a runner without a host registers nowhere.

## `public static Action<Guid> LRunnerRemoveSeam { get; set; } = _ => { };`

Removes a disposed runner from the liveness registry the host owns.
Unattached, it does nothing, so a runner without a host leaves no registry entry to clear.

## `public static int LRunnerLeaseSeam { get; set; } = 10;`

Holds the lease renewal period in seconds, which the host takes from the schedule.
The default matches the schedule value, so an unattached runner renews at the usual pace.

## `public async Task LRunnerDispose()`

Closing a worklist tab cancels any running work first, so its claims return to the queue.
Lease timers stop and the registry entry clears last, so the owner reads gone only once nothing runs.

## `internal void LRunnerLeaseStart(LWorkItem lWorkItem)`

Renews the claim's lease on a timer for as long as the job runs.
So other processes see the owner alive and never reclaim the work mid-encode.
