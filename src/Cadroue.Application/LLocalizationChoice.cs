namespace Cadroue.Application;

public sealed class LLocalizationChoice
{
    private readonly string? lLocalizationChoiceText;

    public LLocalizationChoice(string lLocalizationChoiceToken)
        : this(lLocalizationChoiceToken, string.Empty, null)
    {
    }

    public LLocalizationChoice(string lLocalizationChoiceToken, string lLocalizationChoiceKey)
        : this(lLocalizationChoiceToken, lLocalizationChoiceKey, null)
    {
    }

    public LLocalizationChoice(
        string lLocalizationChoiceToken,
        string lLocalizationChoiceKey,
        string? lLocalizationChoiceText)
    {
        LLocalizationChoiceToken = lLocalizationChoiceToken;
        LLocalizationChoiceKey = lLocalizationChoiceKey;
        this.lLocalizationChoiceText = lLocalizationChoiceText;
    }

    public string LLocalizationChoiceToken { get; }

    public string LLocalizationChoiceKey { get; }

    public static string LLocalizationChoiceRead(object? lLocalizationChoice) =>
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
