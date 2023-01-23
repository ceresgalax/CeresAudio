#ifndef NA_RINGBUFFER_H
#define NA_RINGBUFFER_H
#include <stdint.h>
#include "mutex.h"

struct RingBuffer {
    NAMutex* guard;

    uint8_t* buffer;

    uint32_t capacity;

    uint32_t start;
    uint32_t size;

};
typedef struct RingBuffer RingBuffer;

RingBuffer* rb_new(uint32_t capacity);
void rb_free(RingBuffer* rb);
void rb_init(RingBuffer* rb, uint32_t capacity);
void rb_deinit(RingBuffer* rb);

uint32_t rb_consume(RingBuffer* rb, uint8_t* out_data, uint32_t len);
uint32_t rb_produce(RingBuffer* rb, uint8_t* data, uint32_t len);

uint32_t rb_get_bytes_free(RingBuffer* rb);

#endif // NA_RINGBUFFER_H