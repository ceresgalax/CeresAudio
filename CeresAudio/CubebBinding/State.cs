namespace CeresAudio.CubebBinding
{
    public enum State
    {
        STARTED, // < Stream started.
        STOPPED, // < Stream stopped.
        DRAINED, // < Stream drained.
        ERROR    // < Stream disabled due to error.
    }
}
