namespace Cadroue.Core;

/// <summary>
/// Queue precedence of a scheduled work item. "Add List" schedules at normal
/// precedence; "Execute" schedules at high precedence.
/// </summary>
public enum LWorkPriority
{
    LWorkPriorityNormal,
    LWorkPriorityHigh
}
