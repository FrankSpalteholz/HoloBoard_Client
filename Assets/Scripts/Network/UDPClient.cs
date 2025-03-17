using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;

namespace NetworkFramework
{
    public class UDPClient : NetworkManager
    {
        [Header("Server Settings")]
        [SerializeField] public string serverIP = "192.168.1.100"; // Mac computer IP address
        [SerializeField] private int serverPort = 8080;
        [SerializeField] private int localPort = 8081; // Local port for responses
        
        [Header("Auto-Connect Settings")]
        [SerializeField] private bool autoConnectOnStart = false;
        [SerializeField] public string predefinedIP = ""; // Predefined IP address (manually set)
        [SerializeField] private float autoConnectDelay = 1f; // Short delay for auto-connection
        
        private IPEndPoint serverEndPoint;
        private float startTime;
        private bool autoConnectTriggered = false;
        
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
                Debug.Log("Auto-connecting to: " + serverIP);
            }
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
                Debug.LogError("Client error: " + e.ToString());
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
                    
                    // Process message in main thread
                    HandleIncomingDataOnMainThread(message);
                }
                catch (Exception e)
                {
                    if (isRunning) // Only report errors if not intentionally stopped
                    {
                        MainThreadDispatcher.RunOnMainThread(() => {
                            Debug.LogError("Error receiving data: " + e.ToString());
                            UpdateStatus("Receive error");
                        });
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
                Debug.Log("Message sent: " + message);
            }
            catch (Exception e)
            {
                Debug.LogError("Error sending message: " + e.ToString());
                UpdateStatus("Send error");
            }
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
        
        public override void StopConnection()
        {
            try
            {
                // Send explicit disconnect message if possible
                if (udpClient != null && isRunning && serverEndPoint != null)
                {
                    byte[] data = Encoding.UTF8.GetBytes("__DISCONNECT__");
                    udpClient.Send(data, data.Length, serverEndPoint);
                    Debug.Log("Disconnect message sent");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error sending disconnect message: " + e.ToString());
            }
            
            base.StopConnection();
        }
    }
}