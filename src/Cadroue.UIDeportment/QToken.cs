
namespace Cadroue.UIDeportment;

public enum QTokenKind
{
    QTokenPlain,
    QTokenChip,
    QTokenOperator,
}

public sealed record QTokenPart(
    string QTokenPartText,
    QTokenKind QTokenPartKind,
    string QTokenPartLabel,
    string QTokenPartIcon,
    string QTokenPartCount);

public sealed class QToken
{
    private const string QTokenBackspaceIcon = "/PAsset/PPanel/PTokenBackspace.svg";
    private const string QTokenDeleteIcon = "/PAsset/PPanel/PTokenDelete.svg";

    private static readonly IReadOnlyDictionary<string, string> QTokenLabelKeys = new Dictionary<string, string>
    {
        ["{Prefix}"] = "Token.Prefix.Label",
        ["{OriginalName}"] = "Token.OriginalName.Label",
        ["{SectionNumber}"] = "Token.SectionNumber.Label",
        ["{SectionName}"] = "Token.SectionName.Label",
        ["{Date}"] = "Token.Date.Label",
        ["{Time}"] = "Token.Time.Label",
        ["{Suffix}"] = "Token.Suffix.Label",
    };

    private static readonly IReadOnlyDictionary<string, (string QTokenKey, string QTokenIcon)> QTokenOperators =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Backspace"] = ("Token.Backspace.Label", QTokenBackspaceIcon),
            ["Delete"] = ("Token.Delete.Label", QTokenDeleteIcon),
        };

    public event Action<string>? QTokenInserted;

    public bool QTokenDropHandle(string? lToken)
    {
        if (lToken is null)
        {
            return false;
        }

        QTokenInserted?.Invoke(lToken);
        return true;
    }

    public static IReadOnlyList<QTokenPart> QTokenParse(string lText)
    {
        var lParts = new List<QTokenPart>();
        int lIndex = 0;
        while (lIndex < lText.Length)
        {
            int lStart = lText.IndexOf('{', lIndex);
            if (lStart < 0)
            {
                lParts.Add(QTokenRunCreate(lText[lIndex..]));
                break;
            }

            if (lStart > lIndex)
            {
                lParts.Add(QTokenRunCreate(lText[lIndex..lStart]));
            }

            int lEnd = lText.IndexOf('}', lStart + 1);
            if (lEnd < 0)
            {
                lParts.Add(QTokenRunCreate(lText[lStart..]));
                break;
            }

            lParts.Add(QTokenPartResolve(lText[lStart..(lEnd + 1)]));
            lIndex = lEnd + 1;
        }

        return lParts;
    }

    public static QTokenPart QTokenPartResolve(string lToken)
    {
        string lInner = lToken.Trim('{', '}');
        int lColon = lInner.IndexOf(':');
        string lName = lColon < 0 ? lInner : lInner[..lColon];
        string lCount = lColon < 0 ? "1" : lInner[(lColon + 1)..];
        if (QTokenOperators.TryGetValue(lName, out (string QTokenKey, string QTokenIcon) lOperator))
        {
            return new QTokenPart(
                lToken,
                QTokenKind.QTokenOperator,
                QLocalizationCatalog.QLocalizationTextRead(lOperator.QTokenKey) + lCount,
                lOperator.QTokenIcon,
                lCount);
        }

        string lLabel = QTokenLabelKeys.TryGetValue(lToken, out string? lKey)
            ? QLocalizationCatalog.QLocalizationTextRead(lKey)
            : lInner;
        return new QTokenPart(lToken, QTokenKind.QTokenChip, lLabel, string.Empty, lCount);
    }

    public static string QTokenTextRead(IEnumerable<(string? QTokenText, string? QTokenTag)> lInlines) =>
        string.Concat(lInlines.Select(lInline => lInline.QTokenText ?? lInline.QTokenTag ?? string.Empty))
            .TrimEnd('\r', '\n');

    private static QTokenPart QTokenRunCreate(string lText) =>
        new(lText, QTokenKind.QTokenPlain, lText, string.Empty, string.Empty);
}
