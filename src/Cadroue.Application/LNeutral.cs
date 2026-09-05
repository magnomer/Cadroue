namespace Cadroue.Application;

public enum LNeutralOutcome
{
    LNeutralOutcomeResolved,
    LNeutralOutcomeOutside,
    LNeutralOutcomeEmpty,
    LNeutralOutcomeDark,
    LNeutralOutcomeDecode
}

public enum LNeutralStatus
{
    LNeutralStatusValid,
    LNeutralStatusInvalid,
    LNeutralStatusDecode
}

public enum LNeutralTarget
{
    LNeutralTargetGrey,
    LNeutralTargetWhite
}

public sealed record LNeutralPoint(bool LNeutralPointInside, int LNeutralPointX, int LNeutralPointY);

public sealed record LNeutralDisplay(
    bool LNeutralDisplayVisible,
    bool LNeutralDisplaySampled,
    int LNeutralDisplayRed,
    int LNeutralDisplayGreen,
    int LNeutralDisplayBlue);

public sealed record LNeutralWheel(double LNeutralWheelX, double LNeutralWheelY, bool LNeutralWheelPresent);

public sealed record LNeutralSample(
    LNeutralOutcome LNeutralOutcome,
    int LNeutralRed,
    int LNeutralGreen,
    int LNeutralBlue,
    double LNeutralRedGain,
    double LNeutralGreenGain,
    double LNeutralBlueGain)
{
    public bool LNeutralResolved => LNeutralOutcome == LNeutralOutcome.LNeutralOutcomeResolved;
}
