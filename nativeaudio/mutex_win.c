#ifdef WIN32
#include "mutex.h"
#include <Windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

struct NAMutex {
    CRITICAL_SECTION cs;
};
typedef struct NAMutex NAMutex;

NAMutex* na_mutex_new() {
    NAMutex* mutex = malloc(sizeof(NAMutex));
    memset(mutex, 0, sizeof(NAMutex));
    if (!InitializeCriticalSectionAndSpinCount(&mutex->cs, 0x400)) {
        fprintf(stderr, "Failed to initialize critical section");
        abort();
    }
    return mutex;
}

void na_mutex_free(NAMutex* mutex) {
    na_mutex_enter(mutex);
    DeleteCriticalSection(&mutex->cs);
    free(mutex);
}

void na_mutex_enter(NAMutex* mutex) {
    EnterCriticalSection(&mutex->cs);
}

void na_mutex_exit(NAMutex* mutex) { 
    LeaveCriticalSection(&mutex->cs);
}

#endif // defined(WIN32)