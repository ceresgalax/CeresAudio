#include <string.h>
#include <stdio.h>
#include "nativeaudio.h"
#include "naprivate.h"
#include "ringbuffer.h"
#include "mutex.h"

#define BUFFER_SIZE = 512


struct NAContext {
    cubeb_stream* stream;
    int stream_error;
    cubeb_state state;

    NAMutex* state_guard;
    
    uint32_t channels;

    RingBuffer rb;
};
typedef struct NAContext NAContext;


NATIVEAUDIO_EXPORT NAContext* na_new_context(
    cubeb* cubeb,
    cubeb_device* input_device,
    cubeb_stream_params* input_stream_params,
    cubeb_device* output_device,
    cubeb_stream_params* output_stream_params,
    uint32_t latency_frames
) {
    NAContext* context = malloc(sizeof(NAContext));
    memset(context, 0, sizeof(NAContext));

    context->channels = output_stream_params->channels;

    RingBuffer* rb = &context->rb;
    // TODO: THIS ASSUMES FLOAT
    rb_init(&context->rb, output_stream_params->rate * sizeof(float) * output_stream_params->channels);

    context->state_guard = na_mutex_new();

    context->stream_error = cubeb_stream_init(
        cubeb,
        &context->stream,
        "Ceres Native Audio Stream",
        input_device,
        input_stream_params,
        output_device,
        output_stream_params,
        latency_frames,
        na_data_callback,
        na_state_callback,
        context
    );

    return context;
}

NATIVEAUDIO_EXPORT void na_free_context(NAContext* context) {
    if (context->stream) {
        cubeb_stream_destroy(context->stream);
    }
    rb_deinit(&context->rb);
    na_mutex_free(context->state_guard);
    free(context);
}

NATIVEAUDIO_EXPORT cubeb_stream* na_get_stream(NAContext* context) {
    return context->stream;
}

NATIVEAUDIO_EXPORT int32_t na_get_stream_error(NAContext* context) {
    return context->stream_error;
}

NATIVEAUDIO_EXPORT cubeb_state na_get_stream_state(NAContext* context) {
    na_mutex_enter(context->state_guard);
    cubeb_state state = context->state;
    na_mutex_exit(context->state_guard);
    return state;
}

uint32_t na_get_free_bytes(NAContext* context) {
    return rb_get_bytes_free(&context->rb);
}

NATIVEAUDIO_EXPORT uint32_t na_push(NAContext* context, uint8_t* data, uint32_t len) {
    RingBuffer* rb = &context->rb;
    return rb_produce(rb, data, len);
}

/**
 * PRIVATE FUNCTIONS
 */

long na_data_callback(
    cubeb_stream * stream,
    void * user_ptr,
    void const * input_buffer,
    void * output_buffer,
    long nframes
) {
    NAContext* context = user_ptr;

    // TODO: THIS ASSUMES FLOAT!!
    long buffer_size = nframes * sizeof(float) * context->channels;

    uint32_t bytes_read = rb_consume(&context->rb, output_buffer, buffer_size);

    if (bytes_read > buffer_size) {
        fprintf(stderr, "na_consume returned invalid value");
        abort();
    }

    // Fill the rest of the buffer with zeroes
    long frames_read = bytes_read / (context->channels * sizeof(float));
    long frames_left = nframes - frames_read;
    float* zero_start = (float*)output_buffer + (frames_read * context->channels);
    memset(zero_start, 0, frames_left * context->channels * sizeof(float));

    return nframes;

    // TODO: THIS ASSUMES FLOAT!!
    //return bytes_read / (context->channels * sizeof(float));
}

void na_state_callback(cubeb_stream * stream, void * user_ptr, cubeb_state state) {
    NAContext* context = user_ptr;
    na_mutex_enter(context->state_guard);
    context->state = state;
    na_mutex_exit(context->state_guard);
}


