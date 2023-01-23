using System;
using System.Runtime.InteropServices;

namespace CeresAudio.CubebBinding
{
    public sealed class Cubeb : IDisposable
    {
        internal const string DllName = "cubeb";

        [DllImport(DllName)]
        private static extern Result cubeb_init(out IntPtr context, string context_name, string? backend_name);

        [DllImport(DllName)]
        private static extern Result cubeb_get_min_latency(IntPtr context, ref StreamParams _params, out uint latency_frames);
        
        [DllImport(DllName)]
        private static extern Result cubeb_get_preferred_sample_rate(IntPtr context, out uint rate);
        
        
        private readonly IntPtr _context;

        
        public Cubeb(string contextName, string? backendName)
        {
            Result result = cubeb_init(out _context, contextName, backendName);
            if (result != Result.OK) {
                throw new InvalidOperationException($"Failed to initialize cubeb context: {result}");
            }
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~Cubeb()
        {
            ReleaseUnmanagedResources();
        }
        
        internal IntPtr Handle => _context;

        public uint GetMinLatency(ref StreamParams streamParams)
        {
            Result result = cubeb_get_min_latency(_context, ref streamParams, out uint latencyFrames);
            if (result != Result.OK) {
                throw new InvalidOperationException($"Failed to get min latency: {result}");
            }
            return latencyFrames;
        }

        public uint GetPreferredSampleRate()
        {
            Result result = cubeb_get_preferred_sample_rate(_context, out uint sampleRate);
            if (result != Result.OK) {
                throw new InvalidOperationException($"Failed to get preferred sample rate: {result}");
            }
            return sampleRate;
        }
        
    }
}
