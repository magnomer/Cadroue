using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Convention.Tests;

internal static class TAuditTruthContest
{
    internal static void TAuditContestCheck(
        TAuditTruthField field, List<(SyntaxNode, ExpressionSyntax?, string)> sorted, List<TViolation> violations)
    {
        SyntaxNode? engineWrite = null;
        List<SyntaxNode> plainWrites = [];
        List<(SyntaxNode, ExpressionSyntax?)> written = [];
        foreach ((SyntaxNode reference, ExpressionSyntax? given, string verdict) in sorted)
        {
            if (verdict == "engine")
            {
                engineWrite ??= reference;
            }
            else if (verdict == "plain")
            {
                plainWrites.Add(reference);
            }

            if (verdict != "clear")
            {
                written.Add((reference, given));
            }
        }

        if (engineWrite is not null && plainWrites.Count > 0
            && TAuditLookupWalker.TAuditLookupRead(written) is { } looked)
        {
            (engineWrite, plainWrites) = looked;
        }

        if (engineWrite is not null && plainWrites.FirstOrDefault() is { } plainWrite)
        {
            string where = engineWrite.SyntaxTree == plainWrite.SyntaxTree
                ? $"line {TAuditTruthReference.TAuditLineRead(engineWrite)}"
                : $"{Path.GetFileName(engineWrite.SyntaxTree.FilePath)}:"
                  + $"{TAuditTruthReference.TAuditLineRead(engineWrite)}";
            string others = string.Join(", ", plainWrites.Skip(1).Select(write =>
                $"{Path.GetFileName(write.SyntaxTree.FilePath)}:{TAuditTruthReference.TAuditLineRead(write)}"));
            string also = others.Length > 0 ? $", also at {others}" : string.Empty;
            violations.Add(new TViolation(
                plainWrite.SyntaxTree.FilePath,
                TAuditTruthReference.TAuditLineRead(plainWrite),
                field.TFieldName,
                "Contesting",
                $"written by the engine at {where} and by the shell here{also}"));
        }
    }
}
