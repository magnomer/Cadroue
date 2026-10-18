namespace Cadroue.UIDeportment;

// The source the LosslessCut offer last looked at; it answers only when a new source arrives.
public sealed class QFlowStamp
{
    private string qFlowLosslesscutPath = string.Empty;

    public bool QFlowLosslesscutSet(string lLosslesscutPath)
    {
        if (string.Equals(qFlowLosslesscutPath, lLosslesscutPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        qFlowLosslesscutPath = lLosslesscutPath;
        return true;
    }
}
