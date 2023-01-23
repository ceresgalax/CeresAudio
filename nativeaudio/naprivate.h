#ifndef NA_PRIVATE
#define NA_PRIVATE
#include "mutex.h"

long na_data_callback(
    cubeb_stream * stream,
    void * user_ptr,
    void const * input_buffer,
    void * output_buffer,
    long nframes);

void na_state_callback(cubeb_stream * stream, void * user_ptr, cubeb_state state);

int na_calc_free_bytes(int p, int c, int cap);

#endif // NA_PRIVATE
