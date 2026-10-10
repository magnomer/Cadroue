# TAuditFakeSymbol.cs
Hash: `c82651b1fabf2050`

## `internal static class TAuditFakeSymbol`

Folds a symbol into the one declaration it stands for, and keys it.
The candidate scan, the use scan and the serializer scan share these keys.

## `public static List<ISymbol> TAuditContractRead(ISymbol symbol)`

The interface members one member implements.
A read of an implementation also reads the interface members it stands for.

## `public static ISymbol TAuditNormalRead(ISymbol symbol)`

The one declaration a reference stands for.
An extension call, a partial part, an accessor and a generic instance all fold into it.

## `public static string TAuditKeyRead(ISymbol symbol)`

The documentation id of the folded declaration, which is the same in the source and the test compilation.
