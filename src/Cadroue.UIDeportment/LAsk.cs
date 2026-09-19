namespace Cadroue.UIDeportment;

public sealed record LAsk(string LAskQuestion, string LAskAction)
{
    public string LAskTitle { get; init; } = string.Empty;

    public string? LAskDismiss { get; init; }
}

public static class LAskNotice
{
    public static event Action<LAsk, Action<bool>>? LAskRaise;

    public static void LAskPublish(LAsk? lAsk, Action<bool> lAnswer)
    {
        if (lAsk is null)
        {
            lAnswer(true);
            return;
        }

        if (LAskRaise is not { } lRaise)
        {
            lAnswer(false);
            return;
        }

        lRaise(lAsk, lAnswer);
    }
}
