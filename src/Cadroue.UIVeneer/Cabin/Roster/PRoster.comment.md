# PRoster.cs
Hash: `d045e12a512f08bb`

## `public PRoster(LSceneTabEntry? lPreferenceTabLayout = null)`

Builds the queue and detail columns and subscribes to the QRoster notices.
The elapsed timer starts here and stops only through the close path.

## `public LSceneTabEntry PRosterLayoutRead() => QRoster.QRosterLayoutRead(pRosterLayout.PColumnWeightsRead());`

The roster layout is saved from the live column weights.
The veneer only reports the weights and the deportment decides the stored shape.

## `private void PRosterElapsedTick(object? pSender, EventArgs pArguments) => QRoster.QRosterElapsedTick(IsVisible);`

The visibility flag lets QRoster skip elapsed updates while the worklist tab is hidden.

## `private void PRosterVisibleHandle(object pSender, DependencyPropertyChangedEventArgs pArguments)`

Both options live in the shared preference, so the boxes re-read it whenever visibility changes.

## `private void PRosterDetailDefer() =>`

The rebuild waits at background priority, so it runs after pending input and layout work.

## `private UIElement PRosterPanelBuild()`

The column header padding tracks the queue scrollbar width, so header cells stay above their row cells.
