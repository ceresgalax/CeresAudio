#ifndef NA_MUTEX
#define NA_MUTEX

typedef struct NAMutex NAMutex;

NAMutex* na_mutex_new();
void na_mutex_free(NAMutex* mutex);
void na_mutex_enter(NAMutex* mutex);
void na_mutex_exit(NAMutex* mutex);

#endif // NA_MUTEX