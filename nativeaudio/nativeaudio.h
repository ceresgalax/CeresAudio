#ifndef NATIVEAUDIO_H
#define NATIVEAUDIO_H

#include <stdint.h>
#include "cubeb.h"
#include "nativeaudio_export.h"

typedef struct NAContext NAContext;

NATIVEAUDIO_EXPORT NAContext* na_new_context(
    cubeb* cubeb,
    cubeb_device* input_device,
    cubeb_stream_params* input_stream_params,
    cubeb_device* output_device,
    cubeb_stream_params* output_stream_params,
    uint32_t latency_frames
);

NATIVEAUDIO_EXPORT void na_free_context(NAContext* context);
NATIVEAUDIO_EXPORT cubeb_stream* na_get_stream(NAContext* context);
NATIVEAUDIO_EXPORT int32_t na_get_stream_error(NAContext* context);
NATIVEAUDIO_EXPORT cubeb_state na_get_stream_state(NAContext* context);
NATIVEAUDIO_EXPORT uint32_t na_get_free_bytes(NAContext* context);
NATIVEAUDIO_EXPORT uint32_t na_push(NAContext* context, uint8_t* data, uint32_t len);

#endif // NATIVEAUDIO_H