namespace Cadroue.Application;

// The preview's playback position held inside the video: a seek past the video's end lands on the end.
public static class LPreview
{
    public static TimeSpan LPreviewPositionResolve(
        TimeSpan lPreviewPosition,
        TimeSpan? lPreviewVideoEnd)
    {
        if (lPreviewVideoEnd is not { } lPreviewLimit || lPreviewLimit <= TimeSpan.Zero)
        {
            return lPreviewPosition;
        }

        return lPreviewPosition > lPreviewLimit ? lPreviewLimit : lPreviewPosition;
    }
}
