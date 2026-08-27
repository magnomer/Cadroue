namespace Cadroue.Application;

public static partial class LBridge
{
    private const int LBridgeLeadingCra = 21;
    private const int LBridgeLeadingBla = 18;
    private const int LBridgeVclMax = 31;
    private const int LBridgeLengthSize = 4;

    // A copied middle that follows a head bridge begins mid-stream on the source's
    // first interior keyframe. When that keyframe is an open-GOP CRA it carries RASL
    // leading pictures referencing the discarded pre-cut GOP; a decoder drops them at
    // a true stream start but not after a concatenated head, so it fails to build the
    // reference picture set. Marking only that first CRA as a BLA (broken-link access)
    // makes the decoder discard those leading pictures while every interior CRA keeps
    // its own. The leading pictures fall inside the head bridge's re-encoded range, so
    // nothing user-visible is lost.
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
                // The first coded slice is the copy-start keyframe; only it matters.
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
