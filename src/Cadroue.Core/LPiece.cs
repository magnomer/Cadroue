namespace Cadroue.Core;

public readonly partial record struct LPiece(
    TimeSpan LPieceOrigin,
    TimeSpan LPieceEnd,
    int LPieceColorIndex,
    string LPieceName)
{
    public const int LPieceCeiling = 5000;

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

    public static LPiece LPieceCreate(LSidecarSectionRecord lPieceRecord) =>
        new(
            TimeSpan.FromMilliseconds(lPieceRecord.LSidecarStartMilliseconds),
            TimeSpan.FromMilliseconds(lPieceRecord.LSidecarEndMilliseconds),
            lPieceRecord.LSidecarColorIndex,
            lPieceRecord.LSidecarName ?? string.Empty)
        {
            LPiecePrefix = lPieceRecord.LSidecarPrefix ?? string.Empty,
            LPieceSuffix = lPieceRecord.LSidecarSuffix ?? string.Empty,
            LPieceHidden = lPieceRecord.LSidecarHidden,
            LPieceDetected = lPieceRecord.LSidecarDetected
        };

    public LSidecarSectionRecord LPieceRecordCreate() => new()
    {
        LSidecarStartMilliseconds = (long)LPieceOrigin.TotalMilliseconds,
        LSidecarEndMilliseconds = (long)LPieceEnd.TotalMilliseconds,
        LSidecarColorIndex = LPieceColorIndex,
        LSidecarName = LPieceName,
        LSidecarPrefix = LPiecePrefix,
        LSidecarSuffix = LPieceSuffix,
        LSidecarHidden = LPieceHidden,
        LSidecarDetected = LPieceDetected
    };

    public LSplitSectionDescription LPieceDescribe() =>
        new(LPieceOrigin, LPieceEnd, LPieceName, LPiecePrefix, LPieceSuffix, LPieceHidden);
}
