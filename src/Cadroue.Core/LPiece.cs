namespace Cadroue.Core;

public readonly partial record struct LPiece(TimeSpan LPieceOrigin, TimeSpan LPieceEnd, int LPieceColorIndex, string LPieceName)
{
    private readonly string? lPiecePrefix;
    private readonly string? lPieceSuffix;

    public bool LPieceHidden { get; init; }

    public bool LPieceDetected { get; init; }

    public string LPiecePrefix
    {
        get => lPiecePrefix ?? string.Empty;
        init => lPiecePrefix = value;
    }

    public string LPieceSuffix
    {
        get => lPieceSuffix ?? string.Empty;
        init => lPieceSuffix = value;
    }
}
