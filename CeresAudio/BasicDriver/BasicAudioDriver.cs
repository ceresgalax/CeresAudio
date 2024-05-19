using System.Diagnostics;
using CeresAudio.CubebBinding;

namespace CeresAudio.BasicDriver;

/// <summary>
/// Populate the given samples buffer with the next <see cref="numSamples"/> samples to output.
/// </summary>
/// <param name="samples">The samples buffer to write to.</param>
/// <param name="numSamples">Number of samples. Will always be a multiple of CHANNELS</param>
/// <returns>The number of samples actually populated in the samples buffer.</returns>
public delegate int RenderFunc(float[] samples, int numSamples);

/// <summary>
/// A basic 'driver' for an application using CeresAudio to output audio.
/// The source code of this class is meant as an example of how to user the CeresAudio API.
/// However, this class should also be perfectly usable for a basic audio output use cases.
/// </summary>
public sealed class BasicAudioDriver : IDisposable
{
    public const int CHANNELS = 2;
    
    private RenderFunc _renderer;
    private readonly Action<string>? _logFunc;
    private readonly Cubeb _cubeb;
    private readonly CubebStream _cubebStream;
    private readonly NativeAudio _na;
    private readonly uint _sampleRate;
    private bool _isRunning;
    private readonly object _isRunningLock = new();
    private readonly Thread _renderThread;
    
    private readonly float[] _dataBuffer = new float[1024 * CHANNELS];

    // TODO: Dynamically adjust during runtime -- If buffer underflow, but high sleep time in audio worker, increase update interval.
    private double _updateInterval = 1.0 / 240.0;
    private double _targetLatency = 1.0 / 30.0;

    public uint SampleRate => _sampleRate;
        
    public BasicAudioDriver(Action<string>? logFunc = null)
    {
        _renderer = (_, _) => 0;
        _logFunc = logFunc;
        _cubeb = new Cubeb("CeresAudioBasicAudioDriver", null);
        _sampleRate = _cubeb.GetPreferredSampleRate();
            
        StreamParams outputParams = new StreamParams {
            Channels = CHANNELS,
            ChannelLayout = Channel.UNKNOWN,
            Format = SampleFormat.FLOAT32LE,
            Prefs = StreamPrefs.NONE,
            Rate = _sampleRate
        };

        _na = new NativeAudio(_cubeb, ref outputParams, _cubeb.GetMinLatency(ref outputParams));
        _cubebStream = _na.Stream;

        _renderThread = new Thread(MixerThreadLoop);
        _renderThread.IsBackground = true;
        _renderThread.Priority = ThreadPriority.AboveNormal;
    }

    public void Dispose()
    {
        lock (_isRunningLock) {
            _isRunning = false;
        }
        _renderThread.Join();

        _cubebStream.Dispose();
        _cubeb.Dispose();
    }

    public void Start(RenderFunc renderFunc)
    {
        lock (_isRunningLock) {
            if (_isRunning) {
                throw new InvalidOperationException("Cannot call Start, the driver is already running.");
            }
            _isRunning = true;
        }

        _renderer = renderFunc;
        _cubebStream.Start();
        _renderThread.Start();
    }
    
    private void MixerThreadLoop()
    {
        Stopwatch stopwatch = new();
        
        while (true) {
            stopwatch.Restart();
            
            lock (_isRunningLock) {
                if (!_isRunning) { 
                    break;
                }
            }

            Update();

            TimeSpan sleepTime = TimeSpan.FromSeconds(_updateInterval) - stopwatch.Elapsed;
            if (sleepTime.TotalMilliseconds > 1) {
                Thread.Sleep(sleepTime);
            }
            
        }
    }
    
    private void Update()
    {
        uint capacity = _sampleRate * CHANNELS;

        uint freeSpace = _na.GetFreeSpace<float>();
        if (freeSpace > capacity) {
            throw new InvalidOperationException();
        }

        uint currentLatency = capacity - freeSpace;

        if (currentLatency == 0) {
            _logFunc?.Invoke(nameof(BasicAudioDriver) + ".Update: Buffer was empty!");
        }

        double currentLatencySeconds = currentLatency / (double)(_sampleRate * CHANNELS);

        if (currentLatencySeconds > _targetLatency) {
            return;
        }

        double secondsToMix = _targetLatency - currentLatencySeconds;
        int numSamplesToMix = (int)(secondsToMix * _sampleRate) * CHANNELS;

        int numSamplesMixed = 0;
            
        while (numSamplesMixed < numSamplesToMix) {
            int bufferedLength = Math.Min(_dataBuffer.Length, numSamplesToMix - numSamplesMixed);
            int samplesRead = _renderer(_dataBuffer, bufferedLength);
            if (samplesRead == 0) {
                break;
            }
            
            uint samplesPushed = _na.Push<float>(_dataBuffer, (uint)samplesRead);
            if (samplesPushed < samplesRead) {
                _logFunc?.Invoke("Buffer full");
            }
            
            numSamplesMixed += samplesRead;
        }
            
        if (_na.State == State.DRAINED) {
            _logFunc?.Invoke("Re-starting stream");
            _cubebStream.Start();
        }
    }
        
}
