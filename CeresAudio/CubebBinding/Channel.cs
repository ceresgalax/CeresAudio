using System;

namespace CeresAudio.CubebBinding
{
    [Flags]
    public enum Channel
    {
        UNKNOWN = 0,
        FRONT_LEFT = 1 << 0,
        FRONT_RIGHT = 1 << 1,
        FRONT_CENTER = 1 << 2,
        LOW_FREQUENCY = 1 << 3,
        BACK_LEFT = 1 << 4,
        BACK_RIGHT = 1 << 5,
        FRONT_LEFT_OF_CENTER = 1 << 6,
        FRONT_RIGHT_OF_CENTER = 1 << 7,
        BACK_CENTER = 1 << 8,
        SIDE_LEFT = 1 << 9,
        SIDE_RIGHT = 1 << 10,
        TOP_CENTER = 1 << 11,
        TOP_FRONT_LEFT = 1 << 12,
        TOP_FRONT_CENTER = 1 << 13,
        TOP_FRONT_RIGHT = 1 << 14,
        TOP_BACK_LEFT = 1 << 15,
        TOP_BACK_CENTER = 1 << 16,
        TOP_BACK_RIGHT = 1 << 17
    }
}
