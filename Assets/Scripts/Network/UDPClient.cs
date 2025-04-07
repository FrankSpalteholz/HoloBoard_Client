using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using System.Collections;

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
        
        [Header("Connection Retry Settings")]
        [SerializeField] private bool enableReconnectAttempts = true;
        [SerializeField] private float reconnectInterval = 5f; // Sekunden zwischen Verbindungsversuchen
        [SerializeField] private bool logConnectionErrors = false; // Nur für Debug-Zwecke
        
        private IPEndPoint serverEndPoint;
        private float startTime;
        private bool autoConnectTriggered = false;
        private float lastReconnectTime = 0f;
        private bool isAttemptingConnection = false;
        
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
            // Auto-connect nach kurzer Verzögerung
            if (autoConnectOnStart && !isRunning && !autoConnectTriggered && Time.time > startTime + autoConnectDelay)
            {
                autoConnectTriggered = true;
                ConnectToServer();
            }
            
            // Verbindungswiederherstellung
            if (enableReconnectAttempts && !isRunning && !isAttemptingConnection && Time.time > lastReconnectTime + reconnectInterval)
            {
                if (!string.IsNullOrEmpty(serverIP))
                {
                    lastReconnectTime = Time.time;
                    isAttemptingConnection = true;
                    
                    // Im Hintergrund verbinden, um UI-Blockierung zu vermeiden
                    StartCoroutine(TryReconnect());
                }
            }
        }
        
        private IEnumerator TryReconnect()
        {
            if (logConnectionErrors)
            {
                Debug.Log("Versuche, Verbindung zum Server herzustellen: " + serverIP);
            }
            
            try
            {
                // Starten UDP-Client
                udpClient = new UdpClient(localPort);
                serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);
                
                isRunning = true;
                
                // Starte Thread für Datenempfang
                receiveThread = new Thread(new ThreadStart(ReceiveData));
                receiveThread.IsBackground = true;
                receiveThread.Start();
                
                // Sende Ping (optional)
                byte[] data = Encoding.UTF8.GetBytes("ping");
                udpClient.Send(data, data.Length, serverEndPoint);
                
                // Status aktualisieren
                MainThreadDispatcher.RunOnMainThread(() => {
                    UpdateStatus("Verbindung zum Server wird hergestellt...");
                });
            }
            catch (Exception e)
            {
                // Fehlerbehandlung ohne Debug.LogError
                isRunning = false;
                StopConnectionWithLogging(false); // false bedeutet: keine Fehlermeldungen ausgeben
                
                MainThreadDispatcher.RunOnMainThread(() => {
                    UpdateStatus("Warte auf Server...");
                });
                
                if (logConnectionErrors)
                {
                    Debug.LogWarning("Server nicht erreichbar: " + e.Message);
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
                    
                    // Process message in main thread
                    HandleIncomingDataOnMainThread(message);
                }
                catch (Exception e)
                {
                    if (isRunning) // Only report errors if not intentionally stopped
                    {
                        // Fehler im Hauptthread behandeln, ohne Flut von Meldungen
                        MainThreadDispatcher.RunOnMainThread(() => {
                            // Status aktualisieren ohne Fehlerausgabe
                            UpdateStatus("Verbindung unterbrochen");
                            
                            if (logConnectionErrors)
                            {
                                Debug.LogWarning("Fehler beim Empfang: " + e.Message);
                            }
                        });
                        
                        // Verbindung stoppen, aber nicht ständig neu starten
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
                Debug.Log("Message sent: " + message);
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
        
        // Behalte die ursprüngliche Signatur für die Override-Methode bei
        public override void StopConnection()
        {
            // Rufe einfach deine neue Methode mit dem Standardwert auf
            StopConnectionWithLogging(true);
        }

        // Erstelle eine neue Methode für die erweiterte Funktionalität
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
                        Debug.LogError("Fehler beim Beenden des Threads: " + e.ToString());
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
                        Debug.LogError("Fehler beim Schließen des UdpClient: " + e.ToString());
                    }
                }
                udpClient = null;
            }
            
            UpdateStatus("Verbindung getrennt");
        }
    }
}