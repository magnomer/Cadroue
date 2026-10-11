# Cadroue.Tests.Engine.csproj
Hash: `be754b95abfd4c54`

Builds the tests of Core, Application, Infrastructure, Media and ShellEngine.

## Project references

References `Cadroue.Tests.Interface` alone.
Tests reach production only through the `TInterface` facade.

## Collections

xUnit reads a collection definition only from the test assembly that uses it.
So each collection that must run alone carries its own definition in this assembly.
