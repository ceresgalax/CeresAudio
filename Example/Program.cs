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
