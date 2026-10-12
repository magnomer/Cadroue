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
}
