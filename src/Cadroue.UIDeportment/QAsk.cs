using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed record QAsk(string QAskQuestion, string QAskAction)
{
    public string QAskTitle { get; init; } = string.Empty;

    public string? QAskDismiss { get; init; }
}

public static class QAskNotice
{
    public static event Action<QAsk, Action<bool>>? QAskRaised;

    public static void QAskPublish(QAsk? lAsk, Action<bool> lAnswer)
    {
        if (lAsk is null)
        {
            lAnswer(true);
            return;
        }

        if (QAskRaised is not { } lRaise)
        {
            lAnswer(false);
            return;
        }

        lRaise(lAsk, lAnswer);
    }

    internal static QAsk? QAskCreate(string lQuestionKey, string lActionKey, string lTitleKey) =>
        QAskCreate(() => QLocalizationCatalog.QLocalizationTextRead(lQuestionKey), lActionKey, lTitleKey);

    internal static QAsk? QAskCreate(Func<string> lQuestionRead, string lActionKey, string lTitleKey) =>
        LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive
            ? QAskRecordCreate(lQuestionRead, lActionKey, lTitleKey)
            : null;

    private static QAsk QAskRecordCreate(Func<string> lQuestionRead, string lActionKey, string lTitleKey) =>
        new(
            lQuestionRead(),
            QLocalizationCatalog.QLocalizationTextRead(lActionKey))
        {
            QAskTitle = QLocalizationCatalog.QLocalizationTextRead(lTitleKey)
        };
}
