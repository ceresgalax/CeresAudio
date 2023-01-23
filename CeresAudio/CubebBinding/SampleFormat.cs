namespace CeresAudio.CubebBinding
{
    public enum SampleFormat
    {
        S16LE,     // < Little endian 16-bit signed PCM.
        S16BE,     // < Big endian 16-bit signed PCM.
        FLOAT32LE, // < Little endian 32-bit IEEE floating point PCM.
        FLOAT32BE  // < Big endian 32-bit IEEE floating point PCM.
        
        // Note: Native endian enumerations left out
    }
}
