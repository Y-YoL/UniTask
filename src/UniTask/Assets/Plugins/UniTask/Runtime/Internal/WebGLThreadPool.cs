#if UNITY_WEBGL && !UNITY_EDITOR
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace Cysharp.Threading.Tasks.Internal
{
    /// <summary>
    /// ThreadPool for WebGL
    /// </summary>
    public static class WebGLThreadPool
    {
        private static readonly ConcurrentQueue<(WaitCallback CallBack, object? State)> items = new ConcurrentQueue<(WaitCallback CallBack, object? State)>();
        private static readonly AutoResetEvent queueItemEvent = new AutoResetEvent(false);
        private static bool running = true;
        private static WebGLThread? thread;

        static WebGLThreadPool()
        {
            Application.quitting += OnQuitting;
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="callBack">A <see cref="WaitCallback">WaitCallback</see> that represents the method to be executed.</param>
        public static void QueueUserWorkItem(WaitCallback callBack)
        {
            items.Enqueue((callBack, null));
            MightStartThread();
        }

        /// <summary>
        /// Queues a method for execution. The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="callBack">A <see cref="WaitCallback">WaitCallback</see> that represents the method to be executed.</param>
        /// <param name="state">An object containing data to be used by the method.</param>
        public static void QueueUserWorkItem(WaitCallback callBack, object state)
        {
            items.Enqueue((callBack, state));
            MightStartThread();
        }

        private static void MightStartThread()
        {
            if (thread == null)
            {
                lock (queueItemEvent)
                {
                    if (thread == null)
                    {
                        thread = new WebGLThread(Execute);
                        thread.Start();
                    }
                }
            }

            queueItemEvent.Set();
        }


        private static void Execute()
        {
            while (running)
            {
                if (!items.TryDequeue(out var item))
                {
                    queueItemEvent.WaitOne();
                    continue;
                }

                try
                {
                    item.CallBack.Invoke(item.State);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private static void OnQuitting()
        {
            running = false;
            queueItemEvent.Set();
            if (thread != null)
            {
                thread.Join();
                thread.Dispose();
            }
        }
    }
}
#endif
