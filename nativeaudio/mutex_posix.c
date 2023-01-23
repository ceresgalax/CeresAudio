#ifndef WIN32
#include "mutex.h"
#include <pthread.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

struct NAMutex {
    pthread_mutex_t mutex;
};
typedef struct NAMutex NAMutex;

NAMutex* na_mutex_new() {
    NAMutex* mutex = malloc(sizeof(NAMutex));
    memset(mutex, 0, sizeof(NAMutex));

    if (pthread_mutex_init(&mutex->mutex, NULL)) {
        fprintf(stderr, "Failed to create NAMutex");
        abort();
    }

    return mutex;
}

void na_mutex_free(NAMutex* mutex) {
    na_mutex_enter(mutex);
    if (pthread_mutex_destroy(&mutex->mutex)) {
        fprintf(stderr, "Failed to free NAMutex");
        abort();
    }
    free(mutex);
}

void na_mutex_enter(NAMutex* mutex) {
    if (pthread_mutex_lock(&mutex->mutex)) {
        fprintf(stderr, "Failed to lock NAMutex");
        abort();
    }
}

void na_mutex_exit(NAMutex* mutex) { 
    if (pthread_mutex_unlock(&mutex->mutex)) {
        fprintf(stderr, "Failed to unlock NAMutex");
        abort();
    }
}

#endif // !defined(WIN32)