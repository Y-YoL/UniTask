#include <thread>

typedef void (*ThreadStartWithId)(int);

/// <summary>
/// create and start WebGL thread
/// </summary>
/// <param name="start">invoked function</param>
/// <returns>new thread handle</returns>
extern "C" std::intptr_t __stdcall UniTaskCreateWebGLThread(ThreadStartWithId start, std::int32_t id)
{
    return reinterpret_cast<std::intptr_t>(new std::thread(start, id));
}

/// <summary>
/// join WebGL thread
/// </summary>
/// <param name="handle">thread handle</param>
extern "C" void __stdcall UniTaskJoinWebGLThread(std::intptr_t handle)
{
    auto thread = reinterpret_cast<std::thread *>(handle);
    (*thread).join();
}

/// <summary>
/// delete WebGL thread
/// </summary>
/// <param name="handle">thread handle</param>
extern "C" void __stdcall UniTaskDeleteWebGLThread(std::intptr_t handle)
{
    auto thread = reinterpret_cast<std::thread *>(handle);
    delete thread;
}
