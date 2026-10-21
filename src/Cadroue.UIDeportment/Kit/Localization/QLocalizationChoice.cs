namespace Cadroue.UIDeportment;

public sealed class QLocalizationChoice
{
    public QLocalizationChoice(string qLocalizationChoiceKey)
    {
        ArgumentException.ThrowIfNullOrEmpty(qLocalizationChoiceKey);
        QLocalizationChoiceKey = qLocalizationChoiceKey;
    }

    private string QLocalizationChoiceKey { get; }

    public override string ToString() =>
        QLocalizationCatalog.QLocalizationTextFind(QLocalizationChoiceKey)
        ?? QLocalizationCatalog.QLocalizationFallbackFind(QLocalizationChoiceKey)
        ?? QLocalizationChoiceKey;
}
