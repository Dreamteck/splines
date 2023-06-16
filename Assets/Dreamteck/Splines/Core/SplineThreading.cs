namespace Dreamteck.Splines
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
#if !UNITY_WSA
    using System.Threading;
#endif

    public static class SplineThreading
    {
        public delegate void EmptyHandler();
        public static int threadCount
        {
            get {
#if UNITY_WSA
                return 0;
#else
                return threads.Length;
#endif
            }
            set
            {
#if !UNITY_WSA
                if(value > threads.Length)
                {
                    while (threads.Length < value)
                    {
                        ThreadDef thread = new ThreadDef();
#if UNITY_EDITOR
                        if (Application.isPlaying)
                        {
                            thread.Restart();
                        }
#else
                        thread.Restart();
#endif
                        ArrayUtility.Add(ref threads, thread);
                    }
                }
#endif
            }
        }
#if !UNITY_WSA
        internal class ThreadDef
        {
            internal class Worker
            {
                internal bool computing = false;
                internal Queue<EmptyHandler> instructions = new Queue<EmptyHandler>();
            }
            internal delegate void BoolHandler(bool flag);
            private ParameterizedThreadStart start = null;
            internal Thread thread = null;
            private Worker worker = new Worker();
            internal bool isAlive
            {
                get { return thread != null && thread.IsAlive; }
            }
            internal bool computing
            {
                get
                {
                    return worker.computing;
                }
            }

            internal ThreadDef()
            {
                start = new ParameterizedThreadStart(RunThread);
            }

            internal void Queue(EmptyHandler handler)
            {
                worker.instructions.Enqueue(handler);
            }

            internal void Interrupt()
            {
                thread.Interrupt();
            }

            internal void Restart()
            {
                thread = new Thread(start);
                thread.Start(worker);
            }

            internal void Abort()
            {
                if (isAlive)
                {
                    thread.Abort();
                }
            }
        }
        internal static ThreadDef[] threads = new ThreadDef[2];
        internal static readonly object locker = new object();
        static SplineThreading()
        {
            Application.quitting += Quitting;
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new ThreadDef();
            }

#if UNITY_EDITOR
            PrewarmThreads();
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayStateChanged;
#endif
        }

#if UNITY_EDITOR
        static void OnPlayStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Quitting();
            }
        }
#endif

        static void Quitting()
        {
            Stop();
        }

        static void RunThread(object o)
        {
            ThreadDef.Worker work = (ThreadDef.Worker)o;
            while (true)
            {
                try
                {
                    work.computing = false;
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (ThreadInterruptedException)
                {
                    work.computing = true;
                    lock (locker)
                    {
                        while (work.instructions.Count > 0)
                        {
                            EmptyHandler h = work.instructions.Dequeue();
                            if (h != null) h();
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    if(ex.Message != "") Debug.LogError("THREAD EXCEPTION " + ex.Message);
                    break;
                }
            }
            Debug.Log("Thread stopped");
            work.computing = false;
        }
#endif

            public static void Run(EmptyHandler handler)
        {
#if !UNITY_WSA
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                handler();
                return;
            }
#endif
            for (int i = 0; i < threads.Length; i++)
            {
                if (!threads[i].isAlive) threads[i].Restart();
                if (!threads[i].computing || i == threads.Length - 1)
                {
                    threads[i].Queue(handler);
                    if(!threads[i].computing)threads[i].Interrupt();
                    break;
                }
            }
#endif
        }

        public static void PrewarmThreads()
        {
#if !UNITY_WSA
            for (int i = 0; i < threads.Length; i++)
            {
                if (!threads[i].isAlive)
                {
                    threads[i].Restart();
                }
            }
#endif
        }

        public static void Stop()
        {
#if !UNITY_WSA
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Abort();
            }
#endif
        }
    }
}
