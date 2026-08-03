namespace Cadroue.UIShell.PPanels;

public sealed class PListItem
{
    public PListItem(string pListItemPath, Guid pListItemRelay = default)
    {
        PListItemPath = pListItemPath;
        PListItemRelay = pListItemRelay;
    }

    public string PListItemPath { get; }

    public Guid PListItemRelay { get; set; }

    public bool PListItemDelivered { get; set; }
}
