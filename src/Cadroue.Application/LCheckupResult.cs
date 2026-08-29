using Cadroue.Core;

namespace Cadroue.Application;

public enum LCheckupOutcome
{
    LCheckupOutcomeUntested,
    LCheckupOutcomeScanning,
    LCheckupOutcomeClean,
    LCheckupOutcomeDefect,
    LCheckupOutcomeFailed
}

public readonly record struct LCheckupResult(
    string LCheckupSource,
    LFlawKind LCheckupKind,
    LCheckupOutcome LCheckupOutcome,
    LDossier? LCheckupDossier = null);
