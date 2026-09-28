namespace Convention.Tests;

internal sealed record TAuditObjectRow(
    string TAuditObjectName,
    IReadOnlyList<string> TAuditObjectParts,
    int TAuditObjectLines,
    int TAuditObjectMembers,
    int TAuditObjectState,
    IReadOnlyList<string> TAuditObjectHubs,
    int TAuditObjectCross,
    double TAuditObjectWeave,
    double TAuditObjectFree,
    double TAuditObjectDensity,
    bool TAuditObjectMonolith,
    bool TAuditObjectLarge);
