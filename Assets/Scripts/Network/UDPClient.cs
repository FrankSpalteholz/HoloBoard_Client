using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NetworkFramework
{
    public class UDPClient : NetworkManager
    {
        // Singleton pattern implementation
        public static UDPClient Instance { get; private set; }

        [Header("Server Settings")]
        [SerializeField] public string serverIP = "192.168.1.100"; // Mac computer IP address
        [SerializeField] private int serverPort = 8080;
        [SerializeField] private int localPort = 8081; // Local port for responses
        
        [Header("Auto-Connect Settings")]
        [SerializeField] private bool autoConnectOnStart = false;
        [SerializeField] public string predefinedIP = ""; // Predefined IP address (manually set)
        [SerializeField] private float autoConnectDelay = 1f; // Short delay for auto-connection
        
        [Header("Connection Retry Settings")]
        [SerializeField] private bool enableReconnectAttempts = true;
        [SerializeField] private float reconnectInterval = 5f; // Seconds between connection attempts
        [SerializeField] private bool logConnectionErrors = false; // Only for debug purposes
        
        [Header("Debug Settings")]
        [SerializeField] private int maxLoggedMessages = 10; // Maximum number of messages to keep in log
        
        private IPEndPoint serverEndPoint;
        private float startTime;
        private bool autoConnectTriggered = false;
        private float lastReconnectTime = 0f;
        private bool isAttemptingConnection = false;
        
        // Message log for debugging
        private Queue<LogEntry> messageLog = new Queue<LogEntry>();
        
        // Structure to track message information
        private struct LogEntry
        {
            public string message;
            public MessageDirection direction;
            public DateTime timestamp;
            
            public LogEntry(string msg, MessageDirection dir)
            {
                message = msg;
                direction = dir;
                timestamp = DateTime.Now;
            }
            
            public override string ToString()
            {
                string prefix = direction == MessageDirection.Incoming ? "← IN" : "→ OUT";
                string time = timestamp.ToString("HH:mm:ss");
                return $"[{time}] {prefix}: {message}";
            }
        }
        
        private enum MessageDirection
        {
            Incoming,
            Outgoing
        }

        protected override void Awake()
        {
            base.Awake();
            
            // Singleton implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        protected override void Start()
        {
            base.Start();
            
            // Save start time for delayed auto-connection
            startTime = Time.time;
            
            // Use predefined IP if set
            if (!string.IsNullOrEmpty(predefinedIP))
            {
                // Simple validation to check if it looks like an IP
                string ipText = predefinedIP.Trim();
                if (ipText.Split('.').Length == 4)
                {
                    serverIP = ipText;
                    Debug.Log("Using predefined IP: " + serverIP);
                }
            }
        }
        
        private void Update()
        {
            // Auto-connect after short delay
            if (autoConnectOnStart && !isRunning && !autoConnectTriggered && Time.time > startTime + autoConnectDelay)
            {
                autoConnectTriggered = true;
                ConnectToServer();
            }
            
            // Connection restoration
            if (enableReconnectAttempts && !isRunning && !isAttemptingConnection && Time.time > lastReconnectTime + reconnectInterval)
            {
                if (!string.IsNullOrEmpty(serverIP))
                {
                    lastReconnectTime = Time.time;
                    isAttemptingConnection = true;
                    
                    // Connect in background to avoid UI blocking
                    StartCoroutine(TryReconnect());
                }
            }
        }
        
        private IEnumerator TryReconnect()
        {
            if (logConnectionErrors)
            {
                Debug.Log("Attempting to connect to server: " + serverIP);
            }
            
            try
            {
                // Start UDP client
                udpClient = new UdpClient(localPort);
                serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
                
                isRunning = true;
                
                // Start thread for data reception
                receiveThread = new Thread(new ThreadStart(ReceiveData));
                receiveThread.IsBackground = true;
                receiveThread.Start();
                
                // Send ping (optional)
                byte[] data = Encoding.UTF8.GetBytes("ping");
                udpClient.Send(data, data.Length, serverEndPoint);
                
                // Update status
                MainThreadDispatcher.RunOnMainThread(() => {
                    UpdateStatus("Connecting to server...");
                });
            }
            catch (Exception e)
            {
                // Error handling without Debug.LogError
                isRunning = false;
                StopConnectionWithLogging(false); // false means: don't output error messages
                
                MainThreadDispatcher.RunOnMainThread(() => {
                    UpdateStatus("Waiting for server...");
                });
                
                if (logConnectionErrors)
                {
                    Debug.LogWarning("Server not reachable: " + e.Message);
                }
            }
            
            isAttemptingConnection = false;
            yield return null;
        }
        
        public void ConnectToServer()
        {
            // If already connected, stop first
            if (isRunning)
            {
                StopConnection();
            }
            
            try
            {
                // Create UDP client for the local port
                udpClient = new UdpClient(localPort);
                serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
                
                UpdateStatus("Client initialized, waiting for connection to " + serverIP + ":" + serverPort);
                isRunning = true;
                
                // Start thread for receiving messages
                receiveThread = new Thread(new ThreadStart(ReceiveData));
                receiveThread.IsBackground = true;
                receiveThread.Start();
                
                // Send test connection message
                SendNetworkMessage("Connection request from iPhone");
            }
            catch (Exception e)
            {
                UpdateStatus("Client error: " + e.ToString());
                if (logConnectionErrors)
                {
                    Debug.LogError("Client error: " + e.ToString());
                }
            }
        }
        
        private void ReceiveData()
        {
            IPEndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
            
            while (isRunning)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref senderEndPoint);
                    string message = Encoding.UTF8.GetString(data);
                    
                    // If message comes from server, update status
                    if (senderEndPoint.Address.ToString() == serverIP)
                    {
                        MainThreadDispatcher.RunOnMainThread(() => {
                            UpdateStatus("Connected to " + serverIP + ":" + serverPort);
                        });
                    }
                    
                    // Add to message log
                    AddToMessageLog(message, MessageDirection.Incoming);
                    
                    // Process message in main thread
                    HandleIncomingDataOnMainThread(message);
                }
                catch (Exception e)
                {
                    if (isRunning) // Only report errors if not intentionally stopped
                    {
                        // Handle error in main thread, without flood of messages
                        MainThreadDispatcher.RunOnMainThread(() => {
                            // Update status without error output
                            UpdateStatus("Connection interrupted");
                            
                            if (logConnectionErrors)
                            {
                                Debug.LogWarning("Receive error: " + e.Message);
                            }
                        });
                        
                        // Stop connection, but don't constantly restart
                        isRunning = false;
                        break;
                    }
                }
            }
        }
        
        // Method to send a message to the server
        public void SendNetworkMessage(string message)
        {
            if (udpClient == null || !isRunning)
            {
                UpdateStatus("Not connected - cannot send message");
                return;
            }
            
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                udpClient.Send(data, data.Length, serverEndPoint);
                
                // Add to message log
                AddToMessageLog(message, MessageDirection.Outgoing);
                
                if (logConnectionErrors)
                {
                    Debug.Log("Message sent: " + message);
                }
            }
            catch (Exception e)
            {
                if (logConnectionErrors)
                {
                    Debug.LogError("Error sending message: " + e.ToString());
                }
                UpdateStatus("Send error");
            }
        }
        
        // Add message to log queue with thread safety
        private void AddToMessageLog(string message, MessageDirection direction)
        {
            // Ensure this happens on the main thread
            MainThreadDispatcher.RunOnMainThread(() => {
                // Add new message
                messageLog.Enqueue(new LogEntry(message, direction));
                
                // Maintain maximum size
                while (messageLog.Count > maxLoggedMessages)
                {
                    messageLog.Dequeue();
                }
            });
        }
        
        // Public method to get message log as a formatted string
        public string GetMessageLogAsString()
        {
            if (messageLog.Count == 0)
                return "No messages logged";
                
            StringBuilder sb = new StringBuilder();
            foreach (LogEntry entry in messageLog)
            {
                sb.AppendLine(entry.ToString());
            }
            
            return sb.ToString();
        }
        
        // Clear the message log
        public void ClearMessageLog()
        {
            messageLog.Clear();
        }
        
        public void UpdateServerSettings(string ip, int port)
        {
            serverIP = ip;
            serverPort = port;
            
            if (isRunning)
            {
                // Reconnect with updated settings
                StopConnection();
                ConnectToServer();
            }
        }
        
        // Toggle for auto-connect function
        public void ToggleAutoConnect()
        {
            autoConnectOnStart = !autoConnectOnStart;
            
            if (autoConnectOnStart && !isRunning)
            {
                // Immediately connect if auto-connect is activated and no connection exists
                ConnectToServer();
            }
            
            Debug.Log("Auto-Connect: " + (autoConnectOnStart ? "enabled" : "disabled"));
        }
        
        // Public method to query auto-connect status
        public bool IsAutoConnectEnabled()
        {
            return autoConnectOnStart;
        }
        
        // Keep the original signature for the override method
        public override void StopConnection()
        {
            // Simply call your new method with the default value
            StopConnectionWithLogging(true);
        }

        // Create a new method for the extended functionality
        public void StopConnectionWithLogging(bool logErrors = true)
        {
            try
            {
                // Send explicit disconnect message if possible
                if (udpClient != null && isRunning && serverEndPoint != null)
                {
                    byte[] data = Encoding.UTF8.GetBytes("__DISCONNECT__");
                    udpClient.Send(data, data.Length, serverEndPoint);
                    
                    if (logErrors)
                    {
                        Debug.Log("Disconnect message sent");
                    }
                }
            }
            catch (Exception e)
            {
                if (logErrors)
                {
                    Debug.LogError("Error sending disconnect message: " + e.ToString());
                }
            }
            
            isRunning = false;
            
            if (receiveThread != null && receiveThread.IsAlive)
            {
                try
                {
                    receiveThread.Abort();
                }
                catch (Exception e)
                {
                    if (logErrors)
                    {
                        Debug.LogError("Error terminating thread: " + e.ToString());
                    }
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
                    if (logErrors)
                    {
                        Debug.LogError("Error closing UdpClient: " + e.ToString());
                    }
                }
                udpClient = null;
            }
            
            UpdateStatus("Connection terminated");
        }
    }
}