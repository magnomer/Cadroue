using Cadroue.Application;

namespace Cadroue.UIDeportment;

public enum LTokenKind
{
    LTokenPlain,
    LTokenChip,
    LTokenOperator,
}

public sealed record LTokenPart(
    string LTokenPartText,
    LTokenKind LTokenPartKind,
    string LTokenPartLabel,
    string LTokenPartIcon,
    string LTokenPartCount);

public sealed class LToken
{
    private const string LTokenBackspaceIcon = "/PAsset/PPanel/PTokenBackspace.svg";
    private const string LTokenDeleteIcon = "/PAsset/PPanel/PTokenDelete.svg";

    private static readonly IReadOnlyDictionary<string, string> LTokenLabelKeys = new Dictionary<string, string>
    {
        ["{Prefix}"] = "Token.Prefix.Label",
        ["{OriginalName}"] = "Token.OriginalName.Label",
        ["{SectionNumber}"] = "Token.SectionNumber.Label",
        ["{SectionName}"] = "Token.SectionName.Label",
        ["{Date}"] = "Token.Date.Label",
        ["{Time}"] = "Token.Time.Label",
        ["{Suffix}"] = "Token.Suffix.Label",
    };

    private static readonly IReadOnlyDictionary<string, (string LTokenKey, string LTokenIcon)> LTokenOperators =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Backspace"] = ("Token.Backspace.Label", LTokenBackspaceIcon),
            ["Delete"] = ("Token.Delete.Label", LTokenDeleteIcon),
        };

    public event Action<string>? LTokenInsert;

    public bool LTokenDropHandle(string? lToken)
    {
        if (lToken is null)
        {
            return false;
        }

        LTokenInsert?.Invoke(lToken);
        return true;
    }

    public static IReadOnlyList<LTokenPart> LTokenParse(string lText)
    {
        var lParts = new List<LTokenPart>();
        int lIndex = 0;
        while (lIndex < lText.Length)
        {
            int lStart = lText.IndexOf('{', lIndex);
            if (lStart < 0)
            {
                lParts.Add(LTokenRunCreate(lText[lIndex..]));
                break;
            }

            if (lStart > lIndex)
            {
                lParts.Add(LTokenRunCreate(lText[lIndex..lStart]));
            }

            int lEnd = lText.IndexOf('}', lStart + 1);
            if (lEnd < 0)
            {
                lParts.Add(LTokenRunCreate(lText[lStart..]));
                break;
            }

            lParts.Add(LTokenPartResolve(lText[lStart..(lEnd + 1)]));
            lIndex = lEnd + 1;
        }

        return lParts;
    }

    public static LTokenPart LTokenPartResolve(string lToken)
    {
        string lInner = lToken.Trim('{', '}');
        int lColon = lInner.IndexOf(':');
        string lName = lColon < 0 ? lInner : lInner[..lColon];
        string lCount = lColon < 0 ? "1" : lInner[(lColon + 1)..];
        if (LTokenOperators.TryGetValue(lName, out (string LTokenKey, string LTokenIcon) lOperator))
        {
            return new LTokenPart(
                lToken,
                LTokenKind.LTokenOperator,
                LLocalization.LLocalizationTextRead(lOperator.LTokenKey) + lCount,
                lOperator.LTokenIcon,
                lCount);
        }

        string lLabel = LTokenLabelKeys.TryGetValue(lToken, out string? lKey)
            ? LLocalization.LLocalizationTextRead(lKey)
            : lInner;
        return new LTokenPart(lToken, LTokenKind.LTokenChip, lLabel, string.Empty, lCount);
    }

    public static string LTokenTextRead(IEnumerable<(string? LTokenText, string? LTokenTag)> lInlines) =>
        string.Concat(lInlines.Select(lInline => lInline.LTokenText ?? lInline.LTokenTag ?? string.Empty))
            .TrimEnd('\r', '\n');

    private static LTokenPart LTokenRunCreate(string lText) =>
        new(lText, LTokenKind.LTokenPlain, lText, string.Empty, string.Empty);
}
