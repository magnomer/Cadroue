namespace Cadroue.Application;

public static partial class LBridge
{
    private const int LBridgeLeadingCra = 21;
    private const int LBridgeLeadingBla = 16;
    private const int LBridgeVclMax = 31;
    private const int LBridgeLengthSize = 4;

    public static bool LBridgeLeadingNormalize(byte[] lBridgeBytes)
    {
        int lBridgeMdat = LBridgeMdatFind(lBridgeBytes);
        if (lBridgeMdat < 0)
        {
            return false;
        }

        int lBridgePosition = lBridgeMdat;
        while (lBridgePosition + LBridgeLengthSize < lBridgeBytes.Length)
        {
            long lBridgeLength =
                ((long)lBridgeBytes[lBridgePosition] << 24)
                | ((long)lBridgeBytes[lBridgePosition + 1] << 16)
                | ((long)lBridgeBytes[lBridgePosition + 2] << 8)
                | lBridgeBytes[lBridgePosition + 3];
            int lBridgeHeader = lBridgePosition + LBridgeLengthSize;
            if (lBridgeLength <= 0 || lBridgeHeader >= lBridgeBytes.Length)
            {
                return false;
            }

            int lBridgeType = (lBridgeBytes[lBridgeHeader] >> 1) & 0x3F;
            if (lBridgeType <= LBridgeVclMax)
            {
                if (lBridgeType != LBridgeLeadingCra)
                {
                    return false;
                }

                lBridgeBytes[lBridgeHeader] = (byte)(
                    (lBridgeBytes[lBridgeHeader] & 0x81) | (LBridgeLeadingBla << 1));
                return true;
            }

            lBridgePosition = lBridgeHeader + (int)lBridgeLength;
        }

        return false;
    }

    private static int LBridgeMdatFind(byte[] lBridgeBytes)
    {
        int lBridgePosition = 0;
        while (lBridgePosition + 8 <= lBridgeBytes.Length)
        {
            long lBridgeSize =
                ((long)lBridgeBytes[lBridgePosition] << 24)
                | ((long)lBridgeBytes[lBridgePosition + 1] << 16)
                | ((long)lBridgeBytes[lBridgePosition + 2] << 8)
                | lBridgeBytes[lBridgePosition + 3];
            int lBridgeContent = lBridgePosition + 8;
            bool lBridgeMdat = lBridgeBytes[lBridgePosition + 4] == (byte)'m'
                && lBridgeBytes[lBridgePosition + 5] == (byte)'d'
                && lBridgeBytes[lBridgePosition + 6] == (byte)'a'
                && lBridgeBytes[lBridgePosition + 7] == (byte)'t';
            if (lBridgeSize == 1)
            {
                if (lBridgePosition + 16 > lBridgeBytes.Length)
                {
                    return -1;
                }

                lBridgeSize =
                    ((long)lBridgeBytes[lBridgePosition + 8] << 56)
                    | ((long)lBridgeBytes[lBridgePosition + 9] << 48)
                    | ((long)lBridgeBytes[lBridgePosition + 10] << 40)
                    | ((long)lBridgeBytes[lBridgePosition + 11] << 32)
                    | ((long)lBridgeBytes[lBridgePosition + 12] << 24)
                    | ((long)lBridgeBytes[lBridgePosition + 13] << 16)
                    | ((long)lBridgeBytes[lBridgePosition + 14] << 8)
                    | lBridgeBytes[lBridgePosition + 15];
                lBridgeContent = lBridgePosition + 16;
            }

            if (lBridgeMdat)
            {
                return lBridgeContent;
            }

            if (lBridgeSize < 8)
            {
                return -1;
            }

            lBridgePosition += (int)lBridgeSize;
        }

        return -1;
    }
}
