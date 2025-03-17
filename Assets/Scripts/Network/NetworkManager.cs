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
        public string connectionStatus = "Nicht verbunden";
        
        // Ereignisse für Nachrichtenempfang und Verbindungsstatus
        public event Action<string> OnMessageReceived;
        public event Action<string> OnStatusChanged;
        
        protected virtual void Start()
        {
            DontDestroyOnLoad(this.gameObject);
        }
        
        protected virtual void OnDestroy()
        {
            StopConnection();
        }

        protected virtual void OnApplicationQuit()
        {
            StopConnection();
        }
        
        // Aktualisiert den Verbindungsstatus und löst das entsprechende Ereignis aus
        protected void UpdateStatus(string status)
        {
            connectionStatus = status;
            OnStatusChanged?.Invoke(status);
            Debug.Log("Netzwerkstatus: " + status);
        }
        
        // Verarbeitet eine empfangene Nachricht und löst das entsprechende Ereignis aus
        protected void ProcessReceivedMessage(string message)
        {
            lastReceivedMessage = message;
            OnMessageReceived?.Invoke(message);
            Debug.Log("Nachricht empfangen: " + message);
        }
        
        // Thread-sichere Nachrichtenbehandlung im Unity-Hauptthread
        protected void HandleIncomingDataOnMainThread(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            
            MainThreadDispatcher.RunOnMainThread(() => {
                ProcessReceivedMessage(data);
            });
        }
        
        // Verbindung stoppen und Ressourcen freigeben
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
                    Debug.LogError("Fehler beim Beenden des Threads: " + e.ToString());
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
                    Debug.LogError("Fehler beim Schließen des UdpClient: " + e.ToString());
                }
                udpClient = null;
            }
            
            UpdateStatus("Verbindung getrennt");
        }
    }
}