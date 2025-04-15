using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

namespace NetworkFramework
{
    /// <summary>
    /// Provides functionality to execute actions on the Unity main thread.
    /// This is especially useful for background threads that need to update UI or Unity objects.
    /// </summary>
    public class MainThreadDispatcher : MonoBehaviour
    {
        // Singleton instance
        private static MainThreadDispatcher instance;
        
        // Queue of actions to execute on the main thread
        private static readonly Queue<ActionEntry> executionQueue = new Queue<ActionEntry>();
        
        // Lock object for thread safety
        private static readonly object queueLock = new object();
        
        // Debug counters
        private int actionsExecutedThisFrame = 0;
        private int totalActionsExecuted = 0;
        private int maxActionsPerFrame = 0;
        
        // Structure to track queued actions
        private struct ActionEntry
        {
            public Action action;
            public string description;
            public DateTime queueTime;
            
            public ActionEntry(Action action, string description = null)
            {
                this.action = action;
                this.description = description ?? "Unnamed action";
                this.queueTime = DateTime.Now;
            }
        }
        
        /// <summary>
        /// Get the singleton instance, creating it if necessary
        /// </summary>
        public static MainThreadDispatcher Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<MainThreadDispatcher>();
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
            // Reset the counter for this frame
            actionsExecutedThisFrame = 0;
            
            // Execute actions from the queue on the main thread
            lock(queueLock)
            {
                while (executionQueue.Count > 0)
                {
                    ActionEntry entry = executionQueue.Dequeue();
                    
                    try
                    {
                        // Execute the action
                        entry.action.Invoke();
                        
                        // Update counters
                        actionsExecutedThisFrame++;
                        totalActionsExecuted++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error executing action '{entry.description}': {e.Message}\n{e.StackTrace}");
                    }
                }
            }
            
            // Update max actions per frame if needed
            if (actionsExecutedThisFrame > maxActionsPerFrame)
            {
                maxActionsPerFrame = actionsExecutedThisFrame;
            }
        }
        
        /// <summary>
        /// Executes an action on the Unity main thread.
        /// This is a thread-safe method that can be called from any thread.
        /// </summary>
        /// <param name="action">The action to execute on the main thread</param>
        /// <param name="description">Optional description for debugging purposes</param>
        public static void RunOnMainThread(Action action, string description = null)
        {
            if (Thread.CurrentThread.ManagedThreadId == 1)
            {
                // Already on main thread, execute directly
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error executing action directly on main thread: {e.Message}\n{e.StackTrace}");
                }
            }
            else
            {
                // Queue for later execution on the main thread
                lock(queueLock)
                {
                    executionQueue.Enqueue(new ActionEntry(action, description));
                }
            }
        }
        
        /// <summary>
        /// Get the current status of the dispatcher
        /// </summary>
        /// <returns>A string with information about the current state</returns>
        public string GetStatus()
        {
            return $"Queue Size: {executionQueue.Count}\n" +
                  $"Actions This Frame: {actionsExecutedThisFrame}\n" +
                  $"Total Actions: {totalActionsExecuted}\n" +
                  $"Max Actions/Frame: {maxActionsPerFrame}";
        }
        
        /// <summary>
        /// Check if the current code is running on the main thread
        /// </summary>
        /// <returns>True if running on main thread, false otherwise</returns>
        public static bool IsOnMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == 1;
        }
        
        /// <summary>
        /// Get the current queue size
        /// </summary>
        /// <returns>Number of actions waiting to be executed</returns>
        public int GetQueueSize()
        {
            lock(queueLock)
            {
                return executionQueue.Count;
            }
        }
        
        /// <summary>
        /// Reset the statistics counters
        /// </summary>
        public void ResetStats()
        {
            totalActionsExecuted = 0;
            maxActionsPerFrame = 0;
        }
    }
}