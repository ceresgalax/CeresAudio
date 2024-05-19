using System.Diagnostics;
using CeresAudio;
using CeresAudio.CubebBinding;

namespace Example;

public interface IAudioRenderer
{
    
}

public sealed class BasicAudioDriver : IDisposable
{
    private readonly Cubeb _cubeb;
    private readonly CubebStream _cubebStream;
    private readonly NativeAudio _na;
        
    private readonly uint _sampleRate;
    //private readonly MixingSampleProvider _mixer; // MixingSamplerProvider is thread safe, so we don't need to worry about locking it.
    
    const int CHANNELS = 2;
    
    private bool _isRunning;
    private readonly object _isRunningLock = new();
    private readonly Thread _mixerThread;
    private float _nextSleepTimeCheck;
    
    public TimeSpan PreviousSleepTimeTotal { get; private set; }

    // TODO: Dynamically adjust during runtime -- If buffer underflow, but high sleep time in audio worker, increase update interval.
    private double _updateInterval = 1.0 / 240.0;
    private double _targetLatency = 1.0 / 30.0;
        
    public BasicAudioDriver()
    {
        _cubeb = new Cubeb("CeresGameKit", null);
        _sampleRate = _cubeb.GetPreferredSampleRate();

        Console.WriteLine("AudioSystem: Sample Rate: " + _sampleRate);
            
        //_mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat((int)_sampleRate, CHANNELS));
        //_mixer.ReadFully = true;
            
        StreamParams outputParams = new StreamParams {
            Channels = CHANNELS,
            ChannelLayout = Channel.UNKNOWN,
            Format = SampleFormat.FLOAT32LE,
            Prefs = StreamPrefs.NONE,
            Rate = _sampleRate
        };

        _na = new NativeAudio(_cubeb, ref outputParams, _cubeb.GetMinLatency(ref outputParams));
        _cubebStream = _na.Stream;

        _mixerThread = new Thread(MixerThreadLoop);
        _mixerThread.IsBackground = true;
        _mixerThread.Priority = ThreadPriority.AboveNormal;
    }

    public void Dispose()
    {
        lock (_isRunningLock) {
            _isRunning = false;
        }
        _mixerThread.Join();

        _cubebStream.Dispose();
        _cubeb.Dispose();
    }

    public void Start()
    {
        _cubebStream.Start();

        lock (_isRunningLock) {
            _isRunning = true;
        }
        _mixerThread.Start();
    }
    
    private void MixerThreadLoop()
    {
        Stopwatch stopwatch = new();
        TimeSpan sleepTime = TimeSpan.Zero;
        
        while (true) {
            stopwatch.Restart();
            
            lock (_isRunningLock) {
                if (!_isRunning) { 
                    break;
                }
            }

            Update();

            sleepTime = TimeSpan.FromSeconds(_updateInterval) - stopwatch.Elapsed;
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
            Console.WriteLine(nameof(BasicAudioDriver) + ".Update: Buffer was empty!");
        }

        double currentLatencySeconds = currentLatency / (double)(_sampleRate * CHANNELS);

        if (currentLatencySeconds > _targetLatency) {
            return;
        }

        double secondsToMix = _targetLatency - currentLatencySeconds;
        int numSamplesToMix = (int)(secondsToMix * _sampleRate) * CHANNELS;

        int numSamplesMixed = 0;
            
        while (numSamplesMixed < numSamplesToMix) {
            int bufferedLength = Math.Min(_dataBuffer.Length, (int)(numSamplesToMix - numSamplesMixed));
            // Note: 'samplesRead' is a total of all samples from all channels.
            // For stereo, 1 cubeb sample == 2 mixer samples. ugh.
            int samplesRead = _mixer.Read(_dataBuffer, 0, bufferedLength);
            if (samplesRead == 0) {
                break;
            }
            
            uint samplesPushed = _na.Push<float>(_dataBuffer, (uint)samplesRead);
            if (samplesPushed < samplesRead) {
                Console.WriteLine("Buffer full");
            }
                
            //Console.WriteLine($"Pushed {lenPushed} / {samplesRead}");
            
            numSamplesMixed += samplesRead;
        }
            
        if (_na.State == State.DRAINED) {
            Console.WriteLine("Re-starting stream");
            // TODO: Calling stop in this state crashes on windows. -- Cubeb bug?
            //_cubebStream.Stop();
            _cubebStream.Start();
        }
            
    }

    private readonly float[] _dataBuffer = new float[1024 * CHANNELS];
        
}
