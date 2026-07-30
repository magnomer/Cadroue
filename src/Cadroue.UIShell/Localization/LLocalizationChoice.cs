namespace Cadroue.UIShell;

internal sealed class LLocalizationChoice
{
    private readonly string? lLocalizationChoiceText;

    internal LLocalizationChoice(string lLocalizationChoiceToken)
        : this(lLocalizationChoiceToken, string.Empty, null)
    {
    }

    internal LLocalizationChoice(string lLocalizationChoiceToken, string lLocalizationChoiceKey)
        : this(lLocalizationChoiceToken, lLocalizationChoiceKey, null)
    {
    }

    internal LLocalizationChoice(
        string lLocalizationChoiceToken,
        string lLocalizationChoiceKey,
        string? lLocalizationChoiceText)
    {
        LLocalizationChoiceToken = lLocalizationChoiceToken;
        LLocalizationChoiceKey = lLocalizationChoiceKey;
        this.lLocalizationChoiceText = lLocalizationChoiceText;
    }

    internal string LLocalizationChoiceToken { get; }

    internal string LLocalizationChoiceKey { get; }

    internal static string LLocalizationChoiceRead(object? lLocalizationChoice) =>
        lLocalizationChoice switch
        {
            LLocalizationChoice lLocalizationItem => lLocalizationItem.LLocalizationChoiceToken,
            string lLocalizationText => lLocalizationText,
            _ => string.Empty
        };

    public override string ToString() =>
        lLocalizationChoiceText
        ?? (LLocalizationChoiceKey.Length == 0
            ? LLocalizationChoiceToken
            : LLocalization.LLocalizationTextRead(LLocalizationChoiceKey));
}
