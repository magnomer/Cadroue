namespace Convention.Tests;

internal sealed record TAuditObjectRow(
    string TAuditObjectName,
    int TAuditObjectParts,
    int TAuditObjectLines,
    int TAuditObjectMembers,
    int TAuditObjectMutable,
    int TAuditObjectOutgoing,
    int TAuditObjectIncoming,
    IReadOnlyList<string> TAuditObjectHubs,
    int TAuditObjectShared,
    int TAuditObjectCrossings,
    double TAuditObjectGlued,
    double TAuditObjectFused,
    double TAuditObjectDensity,
    IReadOnlyList<string> TAuditObjectFlags,
    string TAuditObjectVerdict);
