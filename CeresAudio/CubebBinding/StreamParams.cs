using System.Runtime.InteropServices;

namespace CeresAudio.CubebBinding
{
    [StructLayout(LayoutKind.Sequential)]
    public struct StreamParams
    {
        public SampleFormat Format;
        public uint Rate;
        public uint Channels;
        public Channel ChannelLayout;
        public StreamPrefs Prefs;
    }
}
