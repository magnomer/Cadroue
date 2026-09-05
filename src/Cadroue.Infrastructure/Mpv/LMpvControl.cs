namespace Cadroue.Infrastructure;

public sealed partial class LMpv
{
    private const int LMpvFormatDouble = 5;
    private const int LMpvFormatFlag = 3;

    public void LMpvOpen(string lPath)
    {
        LMpvCommandRun("loadfile", lPath);
    }

    public void LMpvSeek(TimeSpan lPosition)
    {
        string lSeconds = lPosition.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        LMpvCommandRun("seek", lSeconds, "absolute+exact");
    }

    public void LMpvStop()
    {
        LMpvCommandRun("stop");
    }

    public void LMpvDecodeInterrupt()
    {
        LMpvCommandRun("stop");
    }

    public void LMpvPlaySet(bool lPlaying)
    {
        LMpvPropertySet("pause", lPlaying ? "no" : "yes");
    }

    public void LMpvVolumeSet(double lVolume)
    {
        double lLinear = Math.Clamp(lVolume, 0, 100) / 100.0;
        double lCurved = 100.0 * Math.Cbrt(lLinear);
        LMpvPropertySet("volume", lCurved.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public void LMpvFilterSet(string lFilterChain)
    {
        LMpvPropertySet("vf", lFilterChain ?? string.Empty);
    }

    public void LMpvAudioSet(string lFilterChain)
    {
        LMpvPropertySet("af", string.IsNullOrEmpty(lFilterChain)
            ? string.Empty
            : "lavfi=[" + lFilterChain + "]");
    }

    public TimeSpan LMpvTimeRead()
    {
        LMpvContextValidate();
        int lResult = LMpvNative.mpv_get_property(lMpvContext, "time-pos", LMpvFormatDouble, out double lSeconds);
        if (lResult < 0 || double.IsNaN(lSeconds) || lSeconds < 0)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(lSeconds);
    }

    public bool LMpvEndedRead()
    {
        LMpvContextValidate();
        int lResult = LMpvNative.mpv_get_property_flag(lMpvContext, "eof-reached", LMpvFormatFlag, out int lFlag);
        return lResult >= 0 && lFlag != 0;
    }
}
