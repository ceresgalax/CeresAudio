cmake_minimum_required(VERSION 3.12.0)
include(GenerateExportHeader)
project(nativeaudio LANGUAGES C VERSION 0.1.0)

include(CTest)
enable_testing()

set(CUBEB_LIB_PATH "" CACHE FILEPATH "Path to cubeb lib to link against")

#  set(NUGET_RUNTIMES_PATH "${CMAKE_SOURCE_DIR}/../CeresAudio/bin/Debug/net6.0/runtimes")

# if (${WIN32})
#     set(CUBEB_LIB_PATH "${NUGET_RUNTIMES_PATH}/win-x64/native/cubeb.lib")
# else()
#     if ("${CMAKE_OSX_ARCHITECTURES}" STREQUAL "arm64")
#         set(CUBEB_LIB_PATH "${NUGET_RUNTIMES_PATH}/osx-arm64/native/libcubeb.dylib")
#     elseif("${CMAKE_OSX_ARCHITECTURES}" STREQUAL "x64")
#         set(CUBEB_LIB_PATH "${NUGET_RUNTIMES_PATH}/osx-x64/native/libcubeb.dylib")
#     else()
#         message(FATAL_ERROR "Unsupported CMAKE_OSX_ARCHITECTURES VALUE ${CMAKE_OSX_ARCHITECTURES}")
#     endif()
    
# endif()

add_library(nativeaudio SHARED nativeaudio.c mutex_posix.c mutex_win.c ringbuffer.c)
generate_export_header(nativeaudio)
install(TARGETS nativeaudio TYPE LIBRARY)
install(FILES ${CMAKE_SOURCE_DIR}/nativeaudio.h TYPE INCLUDE)

# Add build directory as an include directory so we can include the generated export header(s)
target_include_directories(nativeaudio PRIVATE ${CMAKE_BINARY_DIR})
message(STATUS "CMAKE_BINARY_DIR is ${CMAKE_BINARY_DIR}")

target_link_libraries(nativeaudio PUBLIC ${CUBEB_LIB_PATH})
add_compile_definitions(nativeaudio_EXPORTS=1)

#
# Test Target
#
add_executable(natest test.c nativeaudio.c mutex_posix.c mutex_win.c ringbuffer.c)
target_link_libraries(natest PUBLIC ${CUBEB_LIB_PATH})
target_include_directories(natest PRIVATE ${CMAKE_BINARY_DIR})

set(CPACK_PROJECT_NAME ${PROJECT_NAME})
set(CPACK_PROJECT_VERSION ${PROJECT_VERSION})
include(CPack)
