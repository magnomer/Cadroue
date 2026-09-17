namespace Cadroue.UIDeportment;

public sealed class LSLoupe
{
    private bool lLoupePlaying;
    private bool lLoupeEnded;
    private bool lLoupeClosed;
    private string? lLoupeSource;

    public event Action<bool>? LSLoupePlayingChange;

    public bool LSLoupePlaying => lLoupePlaying;

    public bool LSLoupeEnded => lLoupeEnded;

    public bool LSLoupeClosed => lLoupeClosed;

    public string? LSLoupeSource => lLoupeSource;

    public bool LSLoupeResumeCheck() => lLoupePlaying && !lLoupeEnded;

    public void LSLoupeSourceSet(string? lSource) => lLoupeSource = lSource;

    public void LSLoupeClose() => lLoupeClosed = true;

    public void LSLoupeEndSet(bool lEnded) => lLoupeEnded = lEnded;

    public void LSLoupePlayingSet(bool lPlaying)
    {
        lLoupePlaying = lPlaying;
        if (lPlaying)
        {
            lLoupeEnded = false;
        }

        LSLoupePlayingChange?.Invoke(lPlaying);
    }
}
