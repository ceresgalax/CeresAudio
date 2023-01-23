using System;

namespace CeresAudio.CubebBinding
{
    [Flags]
    public enum StreamPrefs
    {
        NONE     = 0x00, // < No stream preferences are requested.
        
        LOOPBACK = 0x01, // < Request a loopback stream. Should be
                         //   specified on the input params and an
                         //   output device to loopback from should
                         //   be passed in place of an input device.
                                           
        DISABLE_DEVICE_SWITCHING = 0x02, // < Disable switching default device on OS changes.
        
        VOICE = 0x04  // < This stream is going to transport voice data.
                      // Depending on the backend and platform, this can
                      // change the audio input or output devices
                      // selected, as well as the quality of the stream,
                      // for example to accomodate bluetooth SCO modes on
                      // bluetooth devices.
    }
}
