namespace Cadroue.UIDeportment;

public static class QWarningNotice
{
    public static event Action<string, string>? QWarningRaised;

    public static void QWarningPublish(string lTitle, string lMessage) => QWarningRaised?.Invoke(lTitle, lMessage);
}
