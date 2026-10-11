namespace Cadroue.UIDeportment;

public sealed class QFlowStamp
{
    private string? lFlowKeyframeStamp;
    private string lFlowLosslesscutPath = string.Empty;

    public string? LFlowKeyframeStamp => lFlowKeyframeStamp;

    public string LFlowLosslesscutPath => lFlowLosslesscutPath;

    public bool LFlowStampSet(string lKeyframeStamp)
    {
        if (string.Equals(lFlowKeyframeStamp, lKeyframeStamp, StringComparison.Ordinal))
        {
            return false;
        }

        lFlowKeyframeStamp = lKeyframeStamp;
        return true;
    }

    public bool LFlowLosslesscutSet(string lLosslesscutPath)
    {
        if (string.Equals(lFlowLosslesscutPath, lLosslesscutPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        lFlowLosslesscutPath = lLosslesscutPath;
        return true;
    }
}
