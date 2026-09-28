# TAuditFakeSerial.cs

## `internal static class TAuditFakeSerial`

Finds what the JSON serializer reads, since it reads by reflection where no walk can see a use.

## `public static void TAuditPersistScan(SemanticModel model, HashSet<string> serialized)`

Every type a `JsonSerializer` call names, by type argument or by argument, is serialized.

## `private static void TAuditPersistAdd(ITypeSymbol? type, HashSet<string> serialized, HashSet<ITypeSymbol> seen)`

Marks a source type and every property of it as serialized, through arrays, type arguments and property types.
The type is marked too, since the serializer constructs it.
