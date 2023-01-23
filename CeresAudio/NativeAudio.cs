using System;
using System.Runtime.InteropServices;
using CeresAudio.CubebBinding;

namespace CeresAudio
{
    public sealed class NativeAudio : IDisposable
    {
        private const string DLL_NAME = "nativeaudio";

        [DllImport(DLL_NAME)]
        private static extern IntPtr na_new_context(
            IntPtr cubeb,
            IntPtr input_device,
            IntPtr input_stream_params,
            IntPtr output_device,
            IntPtr output_stream_params,
            uint latency_frames
        );

        [DllImport(DLL_NAME)]
        private static extern void na_free_context(IntPtr context);

        [DllImport(DLL_NAME)]
        private static extern IntPtr na_get_stream(IntPtr context);
        
        [DllImport(DLL_NAME)]
        private static extern Result na_get_stream_error(IntPtr context);

        [DllImport(DLL_NAME)]
        private static extern State na_get_stream_state(IntPtr context);
        
        [DllImport(DLL_NAME)]
        private static extern uint na_get_free_bytes(IntPtr context);
        
        [DllImport(DLL_NAME)]
        private static extern uint na_push(IntPtr context, IntPtr data, uint len);

        private IntPtr _context;
        public readonly CubebStream Stream;

        public State State => na_get_stream_state(_context);
        
        public NativeAudio(Cubeb cubeb, ref StreamParams outputStreamParams, uint latencyFrames)
        {
            unsafe {
                fixed (void* osp = &outputStreamParams) {
                    _context = na_new_context(cubeb.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, new IntPtr(osp), latencyFrames);        
                }
            }
            
            IntPtr stream = na_get_stream(_context);
            if (stream == IntPtr.Zero) {
                Result result = na_get_stream_error(_context);
                throw new InvalidOperationException($"Error: {result}");
            }
            Stream = new CubebStream(na_get_stream(_context));
        }

        private void ReleaseUnmanagedResources()
        {
            if (_context != IntPtr.Zero) {
                na_free_context(_context);
                _context = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~NativeAudio() {
            ReleaseUnmanagedResources();
        }

        public uint GetFreeSpace<T>()
        {
            uint freeBytes = na_get_free_bytes(_context);
            return freeBytes / (uint)Marshal.SizeOf<T>();
        }

        public uint Push<T>(ReadOnlySpan<T> data, uint len) where T : unmanaged
        {
            if (len > data.Length) {
                throw new ArgumentOutOfRangeException(nameof(len));
            }

            uint pushedBytes;
            unsafe {
                fixed (void* dataP = &data.GetPinnableReference()) {
                    pushedBytes = na_push(_context, new IntPtr(dataP), (uint)sizeof(T) * len);
                }
            }

            return pushedBytes / (uint)Marshal.SizeOf<T>();
        }
    }
}