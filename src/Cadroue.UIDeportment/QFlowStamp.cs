namespace Cadroue.UIDeportment;

public sealed class QFlowStamp
{
    private string? qFlowKeyframeStamp;
    private string qFlowLosslesscutPath = string.Empty;

    public string? QFlowKeyframeStamp => qFlowKeyframeStamp;

    public string QFlowLosslesscutPath => qFlowLosslesscutPath;

    public bool QFlowStampSet(string lKeyframeStamp)
    {
        if (string.Equals(qFlowKeyframeStamp, lKeyframeStamp, StringComparison.Ordinal))
        {
            return false;
        }

        qFlowKeyframeStamp = lKeyframeStamp;
        return true;
    }

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
