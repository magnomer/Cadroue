# Cadroue.Tests.Windows.csproj
Hash: `b1f8687c4da9ca6a`

Builds the tests of the Deportment and the UI relays they use.

## Project references

References `Cadroue.Tests.Interface` and Deportment, never the veneer.
The UI relays live under `Interface/` in a `TInterface` class of their own.
A partial class cannot span assemblies, so the engine facade stays a separate class.
Deportment tests reach that engine facade as `Interface.TInterface`.

## Collections

xUnit reads a collection definition only from the test assembly that uses it.
So each collection that must run alone carries its own definition in this assembly.
