using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NetworkFramework
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher instance;
        private static readonly Queue<Action> executionQueue = new Queue<Action>();
        private static readonly object queueLock = new object();
        
        public static MainThreadDispatcher Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<MainThreadDispatcher>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("MainThreadDispatcher");
                        instance = go.AddComponent<MainThreadDispatcher>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        void Update()
        {
            // Aktionen aus der Queue im Hauptthread ausführen
            lock(queueLock)
            {
                while (executionQueue.Count > 0)
                {
                    executionQueue.Dequeue().Invoke();
                }
            }
        }
        
        /// <summary>
        /// Führt eine Aktion im Unity-Hauptthread aus.
        /// Thread-sichere Methode, die von jedem Thread aufgerufen werden kann.
        /// </summary>
        public static void RunOnMainThread(Action action)
        {
            if (Thread.CurrentThread.ManagedThreadId == 1)
            {
                // Bereits im Hauptthread, direkt ausführen
                action();
            }
            else
            {
                // In die Queue einreihen für spätere Ausführung im Hauptthread
                lock(queueLock)
                {
                    executionQueue.Enqueue(action);
                }
            }
        }
    }
}