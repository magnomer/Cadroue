# Cadroue.UIDeportment.csproj
Hash: `71deaf0aa01faa51`

Builds the deportment, which drives the veneer and decides how the UI carries itself.

## Project ring

References Infrastructure, Media and Conduct, and no WPF, so no control reaches it.
Core, Application and ShellEngine arrive through Conduct, never by a reference of their own.
Transitive references stay on, so the remaining Core and Application uses still compile.
