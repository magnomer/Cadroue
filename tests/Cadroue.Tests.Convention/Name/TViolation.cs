namespace Convention.Tests;

internal sealed record TViolation(
    string TViolationPath,
    int TViolationLine,
    string TViolationName,
    string TViolationKind,
    string TViolationReason);
