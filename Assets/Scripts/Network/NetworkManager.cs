using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;

namespace NetworkFramework
{
    public abstract class NetworkManager : MonoBehaviour
    {
        protected UdpClient udpClient;
        protected Thread receiveThread;
        protected bool isRunning = false;
        
        public string lastReceivedMessage = "";
        public string connectionStatus = "Not connected";
        
        // Events for message reception and connection status
        public event Action<string> OnMessageReceived;
        public event Action<string> OnStatusChanged;
        
        protected virtual void Awake()
        {
            // Base implementation for initialization common to all network managers
            DontDestroyOnLoad(this.gameObject);
        }
        
        protected virtual void Start()
        {
            // Base implementation can be extended by derived classes
        }
        
        protected virtual void OnDestroy()
        {
            StopConnection();
        }

        protected virtual void OnApplicationQuit()
        {
            StopConnection();
        }
        
        // Updates the connection status and triggers the corresponding event
        protected void UpdateStatus(string status)
        {
            connectionStatus = status;
            OnStatusChanged?.Invoke(status);
            Debug.Log("Network status: " + status);
        }
        
        // Processes a received message and triggers the corresponding event
        protected void ProcessReceivedMessage(string message)
        {
            lastReceivedMessage = message;
            OnMessageReceived?.Invoke(message);
            Debug.Log("Message received: " + message);
        }
        
        // Thread-safe message handling in the Unity main thread
        protected void HandleIncomingDataOnMainThread(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            
            MainThreadDispatcher.RunOnMainThread(() => {
                ProcessReceivedMessage(data);
            });
        }
        
        // Stop connection and release resources
        public virtual void StopConnection()
        {
            isRunning = false;
            
            if (receiveThread != null && receiveThread.IsAlive)
            {
                try
                {
                    receiveThread.Abort();
                }
                catch (Exception e)
                {
                    Debug.LogError("Error stopping thread: " + e.ToString());
                }
                receiveThread = null;
            }
            
            if (udpClient != null)
            {
                try
                {
                    udpClient.Close();
                }
                catch (Exception e)
                {
                    Debug.LogError("Error closing UdpClient: " + e.ToString());
                }
                udpClient = null;
            }
            
            UpdateStatus("Connection terminated");
        }
    }
}