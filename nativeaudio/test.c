#include "nativeaudio.h"
#include "naprivate.h"
#include "ringbuffer.h"
#include <stdio.h>

#define TEST(n,c,e) int n##_result = c; if (n##_result != e) {fprintf(stderr, "Failed test %s: Expected %d got %d\n", #n, e, n##_result);} else {fprintf(stdout, "Passed test %s\n", #n);}
#define ASSERT(n, v, expected) int n##_result = v; if (n##_result != expected) { fprintf(stderr, "Failed assertion %s: Expected %d got %d\n", #n, expected, n##_result); return 0; }

int test_initial_free_bytes() {
    RingBuffer* rb = rb_new(8);
    int free = rb_get_bytes_free(rb);
    ASSERT(_1, free, 8);
    return 1;
}

int test_free_after_full_push() {
    RingBuffer* rb = rb_new(8);
    uint8_t* data = malloc(8);
    
    for (int i = 0; i < 8; ++i) {
        data[i] = i + 1;
    }

    rb_produce(rb, data, 8);
    int free = rb_get_bytes_free(rb);
    ASSERT(_1, free, 0);

    rb_consume(rb, data, 8);

    for (int i = 0; i < 8; ++i) {
        ASSERT(_2, data[i], i + 1);
    }

    return 1;
}

int test_try_take_too_much() {
    RingBuffer* rb = rb_new(8);
    uint8_t* data = malloc(8);
    
    for (int i = 0; i < 8; ++i) {
        data[i] = i + 1;
    }

    int produced = rb_produce(rb, data, 4);
    ASSERT(_produced, produced, 4);
    int free = rb_get_bytes_free(rb);
    ASSERT(_1, free, 4);

    uint32_t taken = rb_consume(rb, data, 8);
    ASSERT(_2, taken, 4);

    for (int i = 0; i < 4; ++i) {
        ASSERT(_3, data[i], i + 1);
    }

    return 1;
}

int test_loop() {
    RingBuffer* rb = rb_new(8);
    uint8_t* data = malloc(12);
    
    for (int i = 0; i < 12; ++i) {
        data[i] = i + 1;
    }

    uint8_t* out_data = malloc(12);

    uint32_t produced = rb_produce(rb, data, 6);
    ASSERT(_produced1, produced, 6);
    uint32_t free = rb_get_bytes_free(rb);
    ASSERT(_1, free, 2);

    uint32_t consumed = rb_consume(rb, out_data, 4);
    ASSERT(_consumed1, consumed, 4);

    produced = rb_produce(rb, data, 6);
    ASSERT(_produced2, produced, 6);

    return 1;
}

int main(int argc, const char** argv) {
    TEST(initial_free_bytes, test_initial_free_bytes(), 1);
    TEST(free_after_full_push, test_free_after_full_push(), 1);
    TEST(take_too_much, test_try_take_too_much(), 1);
    TEST(loop, test_loop(), 1);
    return 0;
}