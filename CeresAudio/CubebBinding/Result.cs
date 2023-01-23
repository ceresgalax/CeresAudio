namespace CeresAudio.CubebBinding
{
    public enum Result
    {
        OK = 0,                       // **< Success.
        ERROR = -1,                   // **< Unclassified error.
        ERROR_INVALID_FORMAT = -2,    // **< Unsupported #cubeb_stream_params requested.
        ERROR_INVALID_PARAMETER = -3, // **< Invalid parameter specified.
        ERROR_NOT_SUPPORTED = -4,     // **< Optional function not implemented in current backend.
        ERROR_DEVICE_UNAVAILABLE = -5 // **< Device specified by #cubeb_devid not available.
    }
}
