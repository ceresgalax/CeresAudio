using System;
using System.Runtime.InteropServices;

namespace CeresAudio.CubebBinding
{
    public sealed class CubebStream : IDisposable
    {
        private delegate long cubeb_data_callback(IntPtr stream, IntPtr user_ptr, IntPtr input_buffer, IntPtr output_buffer, long nframes);
        private delegate void cubeb_state_callback(IntPtr stream, IntPtr user_ptr, State state);
        
        [DllImport(Cubeb.DllName)]
        private static extern Result cubeb_stream_init(
                IntPtr context,
                out IntPtr stream,
                string stream_name,
                IntPtr input_device,
                IntPtr input_stream_params,
                IntPtr output_device,
                IntPtr output_stream_params,
                uint latency_frames,
                cubeb_data_callback data_callback,
                cubeb_state_callback state_callback,
                IntPtr user_ptr);

        [DllImport(Cubeb.DllName)]
        private static extern Result cubeb_stream_start(IntPtr stream);
        
        [DllImport(Cubeb.DllName)]
        private static extern Result cubeb_stream_stop(IntPtr stream);
        
        private readonly IntPtr _handle;
        private readonly IStreamDelegate? _delegate;
        private GCHandle _userDataHandle;
        private readonly SampleFormat _inputFormat;
        private readonly SampleFormat _outputFormat;
        private readonly uint _inputChannels;
        private readonly uint _outputChannels;

        private readonly cubeb_data_callback? _dataCallback;
        private readonly cubeb_state_callback? _stateCallback;

        public CubebStream(IntPtr handle)
        {
            if (handle == IntPtr.Zero) {
                throw new ArgumentOutOfRangeException(nameof(handle));
            }
            _handle = handle;
        }
        
        public CubebStream(
                Cubeb context,
                string streamName,
                bool supportInput,
                ref StreamParams inputStreamParams,
                bool supportOutput,
                ref StreamParams outputStreamParams,
                uint latencyFrames,
                IStreamDelegate streamDelegate)
        {
            Result result;

            _delegate = streamDelegate;
            
            _userDataHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            
            _dataCallback = HandleData;
            _stateCallback = HandleState;

            _inputFormat = inputStreamParams.Format;
            _outputFormat = outputStreamParams.Format;
            _inputChannels = inputStreamParams.Channels;
            _outputChannels = outputStreamParams.Channels;

            unsafe {
                fixed (StreamParams* inputStreamParamsPtr = &inputStreamParams)
                fixed (StreamParams* outputStreamParamsPtr = &outputStreamParams) {
                    result = cubeb_stream_init(
                        context: context.Handle,
                        stream: out _handle,
                        stream_name: streamName,
                        input_device: IntPtr.Zero,
                        input_stream_params: supportInput ? (IntPtr)inputStreamParamsPtr : IntPtr.Zero,
                        output_device: IntPtr.Zero,
                        output_stream_params: supportOutput ? (IntPtr)outputStreamParamsPtr : IntPtr.Zero,
                        latency_frames: latencyFrames,
                        data_callback: _dataCallback,
                        state_callback: _stateCallback,
                        user_ptr: GCHandle.ToIntPtr(_userDataHandle));
                }
            }

            if (result != Result.OK) {
                _userDataHandle.Free();
                throw new InvalidOperationException($"Failed to init cubeb stream: {result}");
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

        ~CubebStream()
        {
            ReleaseUnmanagedResources();
        }

        public void Start()
        {
            Result result = cubeb_stream_start(_handle);
            if (result != Result.OK) {
                throw new InvalidOperationException($"Failed to start stream: {result}");
            }
        }

        public void Stop()
        {
            Result result = cubeb_stream_stop(_handle);
            if (result != Result.OK) {
                throw new InvalidOperationException($"Failed to stop stream: {result}");
            }
        }
        
        private static long HandleData(IntPtr stream, IntPtr user_ptr, IntPtr input_buffer, IntPtr output_buffer, long nframes)
        {
            CubebStream managedStream = (CubebStream) GCHandle.FromIntPtr(user_ptr).Target!;
            var buffers = new BufferProvider(managedStream, input_buffer, output_buffer, nframes, managedStream._inputChannels, managedStream._outputChannels);
            return managedStream._delegate!.ProcessData(managedStream, buffers);
        }

        private static void HandleState(IntPtr stream, IntPtr user_ptr, State state)
        {
            CubebStream managedStream = (CubebStream) GCHandle.FromIntPtr(user_ptr).Target!;
            managedStream._delegate!.ProcessState(managedStream, state);
        }

        public readonly ref struct BufferProvider
        {
            private readonly CubebStream _stream;
            private readonly IntPtr _inputBuffer;
            private readonly IntPtr _outputBuffer;
            public readonly long NumFrames;
            private readonly uint _numInputChannels;
            private readonly uint _numOutputChannels;

            public BufferProvider(CubebStream stream, IntPtr inputBuffer, IntPtr outputBuffer, long nframes,
                uint numInputChannels, uint numOutputChannels)
            {
                _stream = stream;
                _inputBuffer = inputBuffer;
                _outputBuffer = outputBuffer;
                NumFrames = nframes;
                _numInputChannels = numInputChannels;
                _numOutputChannels = numOutputChannels;
            }

            public unsafe ReadOnlySpan<T> GetInputBuffer<T>()
            {
                AssertType<T>(_stream._inputFormat);
                return new ReadOnlySpan<T>((void *) _inputBuffer, (int)(NumFrames * _numInputChannels));
            }

            public unsafe Span<T> GetOutputBuffer<T>()
            {
                AssertType<T>(_stream._outputFormat);
                return new Span<T>((void*) _outputBuffer, (int)(NumFrames * _numOutputChannels));
            }
            
            private void AssertType<T>(SampleFormat format)
            {
                Type t = typeof(T);
                
                switch (format) {
                    case SampleFormat.S16LE: 
                    case SampleFormat.S16BE:
                        if (t == typeof(short)) {
                            return;
                        }
                        break;
                    case SampleFormat.FLOAT32LE:
                    case SampleFormat.FLOAT32BE:
                        if (t == typeof(float)) {
                            return;
                        }
                        break;
                }

                throw new InvalidOperationException("Incorrect type of buffer requested");
            }
        }

        
    }
}
