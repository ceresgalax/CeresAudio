namespace CeresAudio.CubebBinding
{
    public interface IStreamDelegate
    {
        long ProcessData(CubebStream stream, CubebStream.BufferProvider buffers);
        void ProcessState(CubebStream stream, State state);
    }
}
