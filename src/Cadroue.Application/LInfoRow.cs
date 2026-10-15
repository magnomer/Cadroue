namespace Cadroue.Application;

public sealed record LInfoRow(string LInfoRowKind, string LInfoRowCaption)
{
    public const string LInfoRowMuted = "Muted";
    public const string LInfoRowText = "Text";
    public const string LInfoRowGrey = "Grey";
    public const string LInfoRowGood = "Good";
    public const string LInfoRowBad = "Bad";
    public const string LInfoRowSeparator = "Separator";
}
