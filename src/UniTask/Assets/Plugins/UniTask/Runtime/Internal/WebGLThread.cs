#if UNITY_WEBGL
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using AOT;

namespace Cysharp.Threading.Tasks.Internal
{
    /// <summary>
    /// Creates and controls a WebGL thread.
    /// </summary>
    internal sealed class WebGLThread : IDisposable
    {
        private static readonly ConcurrentDictionary<int, WebGLThread> threads = new ConcurrentDictionary<int, WebGLThread>();
        private static int currentId;
        private readonly ThreadStart start;
        private readonly int id;

        /// <summary>
        /// std::thread pointer
        /// </summary>
        private IntPtr handle;

        private delegate void ThreadStartWithId(int id);

        /// <summary>
        /// Initializes a new instance of the Thread class.
        /// </summary>
        /// <param name="start">A ThreadStart delegate that represents the methods to be invoked when this thread begins executing.</param>
        public WebGLThread(ThreadStart start)
        {
            this.id = Interlocked.Increment(ref currentId);
            this.start = start;
            threads[this.id] = this;
            this.ThreadState = ThreadState.Unstarted;
        }

        /// <summary>
        /// Gets a value containing the states of the current thread.
        /// </summary>
        public ThreadState ThreadState { get; private set; }

        ~WebGLThread()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Causes the operating system to change the state of the current instance to Running.
        /// </summary>
        public void Start()
        {
            if (this.ThreadState != ThreadState.Unstarted)
            {
                throw new ThreadStateException("Thread is running or terminated. Cannot restart.");
            }

            this.handle = UniTaskCreateWebGLThread(Execute, this.id);
        }

        /// <summary>
        /// Blocks the calling thread until the thread represented by this instance terminates.
        /// </summary>
        public void Join()
        {
            if (this.ThreadState == ThreadState.Unstarted)
            {
                throw new ThreadStateException("Thread is unstarted. Cannot Join.");
            }

            this.ThreadState = ThreadState.WaitSleepJoin;
            UniTaskJoinWebGLThread(this.handle);
        }

        [MonoPInvokeCallback(typeof(ThreadStartWithId))]
        private static void Execute(int id)
        {
            if (!threads.TryRemove(id, out var thread))
            {
                return;
            }

            try
            {
                thread.ThreadState = ThreadState.Running;
                thread.start.Invoke();
            }
            finally
            {
                thread.ThreadState = ThreadState.Stopped;
            }
        }

        private void Dispose(bool disposing)
        {
            threads.TryRemove(this.id, out _);
            if (this.handle != IntPtr.Zero)
            {
                UniTaskDeleteWebGLThread(this.handle);
                this.handle = IntPtr.Zero;
            }
        }

        [DllImport("__Internal")]
        private static extern IntPtr UniTaskCreateWebGLThread(ThreadStartWithId start, int id);

        [DllImport("__Internal")]
        private static extern void UniTaskJoinWebGLThread(IntPtr handle);

        [DllImport("__Internal")]
        private static extern void UniTaskDeleteWebGLThread(IntPtr handle);
    }
}
#endif
