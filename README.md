# CeresAudio
CeresAudio is a simple .NET library for cross-platform audio output. It heavily relies on Mozilla's excellent 'cubeb'
library. 

Instead of simply providing C# bindings for cubeb, CeresAudio instead implements cubeb audio callbacks and a
ring buffer in C, allowing for the audio callback to run with as little latency as possible.
CeresAudio provides a simple thread-safe C# interface for filling this ring buffer with rendered audio.

# Usage
## API usage example
```c# 
Cubeb cubeb = new("Example", null);
uint sampleRate = cubeb.GetPreferredSampleRate();

StreamParams outputParams = new StreamParams {
    Channels = 2,
    ChannelLayout = Channel.UNKNOWN,
    Format = SampleFormat.FLOAT32LE,
    Prefs = StreamPrefs.NONE,
    Rate = _sampleRate
};

// Create a NativeAudio object, which manages a ringbuffer and cubeb stream configured to pull from that ring buffer.
// NOTE: The created ring buffer is appropriately sized based on the outputParams.
NativeAudio na = new(cubeb, ref outputParams, cubeb.GetMinLatency(ref outputParams));
CubebStream cubebStream = na.Stream;
cubebStream.Start();

// Figure out how many samples the ring buffer has free
uint freeSpace = na.GetFreeSpace<float>();

// Push samples into the ring buffer
// (This will likely be done in a background thread)
uint samplesPushed = na.Push<float>(_dataBuffer, (uint)samplesRead);

// Clean up!
cubebStream.Dispose();
cubeb.Dispose();
na.Dispose();

```

## Using the high-level BasicAudioDriver
The source code of the BasicAudioDriver class is included as reference for how to write an audio driver using CeresAudio.
It can also be used as an out-of-the-box audio driver for your application.
```c#
//
// This example will play a middle-c pitched sine wave tone for 5 seconds. 
//

using CeresAudio.BasicDriver;

const float middleCHertz = 261.626f;
const float amplitude = 0.5f;

BasicAudioDriver driver = new(logFunc: Console.WriteLine);

float step = (MathF.PI * middleCHertz) / driver.SampleRate;
float theta = 0f;

RenderFunc renderFunc = (samples, numSamples) => {
    for (int i = 0; i < numSamples; ++i) {
        samples[i] = MathF.Sin(theta) * amplitude;
        theta += step;
    }
    return numSamples;
};

driver.Start(renderFunc);
await Task.Delay(5000);
```

Note: For my game projects using CeresAudio, I have been using [NAudio](https://github.com/naudio/NAudio) as a mixer for
multiple audio sources. You can use `MixingSampleProvider` and call `mixingSampleProvider.Read(samples, 0, numSamples)` 
in your render func to output mixed audio.  

# Using CeresAudio in your .NET project.
Currently, the best way to use CeresAudio is to clone this repository as a git submodule or copy the
CeresAudio source files into your project, and then include the CeresAudio .csproj in your solution.

When including CeresAudio this way, you will need to have `cmake` installed so that the native code can be compiled.

In the future, I would like to maintain NuGet packages with the native code included.

# FAQ

**Q: Why not implement C# bindings for the Cubeb audio callback and just pause the garbage collector during the callback?**

A: I attempted this at first, but interop between native and .NET was still very expensive. 
I was also not able to find a bulletproof way to prevent garbage collection from occuring.
Note that this may be improved in newer versions of the .NET runtime. However, having as little latency as possible is
ideal for the cubeb audio callback, so keeping the audio callback outside of .NET keeps this callback function
as performant as possible.
