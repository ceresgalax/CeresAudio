#include "ringbuffer.h"
#include <string.h>
#include <stdlib.h>
#include <stdio.h>

RingBuffer* rb_new(uint32_t capacity) {
    RingBuffer* rb = malloc(sizeof(RingBuffer));
    memset(rb, 0, sizeof(RingBuffer));
    rb_init(rb, capacity);
    return rb;
}

void rb_free(RingBuffer* rb) {
    rb_deinit(rb);
    free(rb);
}

void rb_init(RingBuffer* rb, uint32_t capacity) {
    rb->guard = na_mutex_new();
    rb->capacity = capacity;
    rb->buffer = malloc(rb->capacity);
}

void rb_deinit(RingBuffer* rb) {
    na_mutex_free(rb->guard);
}

#define ASSERT(c) if (!(c)) { fprintf(stderr, "ASSERTION FAILED: %s:%d: %s\n", __FILE__, __LINE__, #c); abort(); }

uint32_t rb_produce(RingBuffer* rb, uint8_t* data, uint32_t len) {
    uint8_t* buffer = rb->buffer;
    int capacity = rb->capacity;
    
    na_mutex_enter(rb->guard);
    int start = rb->start;
    int size = rb->size;
    na_mutex_exit(rb->guard);

    ASSERT(size <= capacity);
    ASSERT(start < capacity);
    
    //int free = capacity - size + start;
    int free = capacity - size;
    int numBytesToCopy = len < free ? len : free;
    
    int firstCopyOffset = (start + size) % capacity;
    int firstCopySize = capacity - firstCopyOffset;
    firstCopySize = firstCopySize > numBytesToCopy ? numBytesToCopy : firstCopySize;
    int secondCopySize = numBytesToCopy - firstCopySize;

    ASSERT(firstCopyOffset + firstCopySize <= capacity);
    ASSERT(secondCopySize <= capacity);
    ASSERT(firstCopySize + secondCopySize <= len);

    memcpy(buffer + firstCopyOffset, data, firstCopySize);
    memcpy(buffer, data + firstCopySize, secondCopySize);

    na_mutex_enter(rb->guard);
    rb->size += numBytesToCopy;
    ASSERT(rb->size <= capacity);
    na_mutex_exit(rb->guard);
    
    return numBytesToCopy;
}

uint32_t rb_consume(RingBuffer* rb, uint8_t* out_data, uint32_t len) {
    uint8_t* buffer = rb->buffer;
    int capacity = rb->capacity;
    
    na_mutex_enter(rb->guard);
    int start = rb->start;
    int size = rb->size;
    na_mutex_exit(rb->guard);
    
    int numBytesToCopy = len < size ? len : size;
    
    int firstCopySize = capacity - start;
    firstCopySize = firstCopySize > numBytesToCopy ? numBytesToCopy : firstCopySize;
    int secondCopySize = numBytesToCopy - firstCopySize;

    ASSERT(start + firstCopySize <= capacity);
    ASSERT(secondCopySize <= capacity);
    ASSERT(firstCopySize + secondCopySize <= len);

    memcpy(out_data, buffer + start, firstCopySize);
    memcpy(out_data + firstCopySize, buffer, secondCopySize);

    na_mutex_enter(rb->guard);
    rb->start = (rb->start + numBytesToCopy) % capacity;
    rb->size -= numBytesToCopy;
    ASSERT(rb->size <= capacity)
    ASSERT(rb->start < capacity);
    na_mutex_exit(rb->guard);
    
    return numBytesToCopy;
}

uint32_t rb_get_bytes_free(RingBuffer* rb) {
    na_mutex_enter(rb->guard);
    uint32_t size = rb->capacity - rb->size;
    na_mutex_exit(rb->guard);
    return size;
}