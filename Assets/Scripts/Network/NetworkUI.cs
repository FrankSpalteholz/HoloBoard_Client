using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;

namespace NetworkFramework
{
    public class NetworkUI : MonoBehaviour
    {
        [Header("Network Manager References")]
        [SerializeField] private MonoBehaviour networkManager; // Cast to NetworkManager
        [SerializeField] private MonoBehaviour udpClient; // Cast to UDPClient, only for iPhone
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI messageLogText;
        [SerializeField] private TMP_InputField messageInputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private TMP_InputField ipAddressInput;
        [SerializeField] private TMP_InputField portInput;
        
        [Header("Status Color Settings")]
        [SerializeField] private Color connectedColor = Color.green;
        [SerializeField] private Color disconnectedColor = Color.red;
        [SerializeField] private Color waitingColor = Color.yellow;
        
        [Header("Font Size Settings")]
        [SerializeField] private int statusFontSize = 24;
        [SerializeField] private int messageLogFontSize = 18;
        
        private ScrollRect scrollRect;
        
        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
            
            // Find ScrollRect if available
            if (messageLogText != null)
            {
                Transform parent = messageLogText.transform.parent;
                while (parent != null && scrollRect == null)
                {
                    scrollRect = parent.GetComponent<ScrollRect>();
                    if (parent.parent != null)
                        parent = parent.parent;
                    else
                        break;
                }
            }
            
            // Set initial font sizes
            if (statusText != null)
                statusText.fontSize = statusFontSize;
            
            if (messageLogText != null)
                messageLogText.fontSize = messageLogFontSize;
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void InitializeUI()
        {
            // Check if this is a client
            UDPClient clientComponent = udpClient as UDPClient;
            bool isClient = clientComponent != null;
            
            // Initialize UI elements based on device type
            if (connectButton != null)
                connectButton.gameObject.SetActive(isClient);
                
            if (ipAddressInput != null)
                ipAddressInput.gameObject.SetActive(isClient);
            
            // For client UI: Configure IP field with default
            if (isClient && ipAddressInput != null && clientComponent != null)
            {
                // Use currently configured IP
                ipAddressInput.text = clientComponent.serverIP;
            }
            
            if (portInput != null)
            {
                portInput.text = "8080"; // Default port
            }
            
            // Connect UI elements with actions
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(SendMessage);
            }
            
            if (connectButton != null && isClient)
            {
                connectButton.onClick.AddListener(() => {
                    UDPClient clientComponent = udpClient as UDPClient;
                    if (clientComponent != null && ipAddressInput != null)
                    {
                        // Update IP address and port, then establish connection
                        clientComponent.serverIP = ipAddressInput.text;
                        
                        // Get port from input field if available
                        int port = 8080;
                        if (portInput != null && int.TryParse(portInput.text, out int newPort))
                        {
                            port = newPort;
                        }
                        
                        clientComponent.UpdateServerSettings(ipAddressInput.text, port);
                        clientComponent.ConnectToServer();
                    }
                });
            }
            
            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(() => {
                    NetworkManager netManager = networkManager as NetworkManager;
                    if (netManager != null)
                    {
                        netManager.StopConnection();
                    }
                });
            }
            
            // Initialize status
            UpdateStatusText("Not connected");
        }
        
        private void SubscribeToEvents()
        {
            if (networkManager != null)
            {
                // Cast to correct class and subscribe to events
                NetworkManager netManager = networkManager as NetworkManager;
                if (netManager != null)
                {
                    netManager.OnStatusChanged += UpdateStatusText;
                    netManager.OnMessageReceived += AddMessageToLog;
                }
                else
                {
                    Debug.LogError("NetworkManager reference is not a NetworkManager object!");
                }
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (networkManager != null)
            {
                // Cast to correct class and unsubscribe from events
                NetworkManager netManager = networkManager as NetworkManager;
                if (netManager != null)
                {
                    netManager.OnStatusChanged -= UpdateStatusText;
                    netManager.OnMessageReceived -= AddMessageToLog;
                }
            }
        }
        
        private void UpdateStatusText(string status)
        {
            if (statusText != null)
            {
                statusText.text = "Status: " + status;
                
                // Update status color based on connection status
                if (status.Contains("Connected") || status.Contains("started"))
                    statusText.color = connectedColor;
                else if (status.Contains("Error") || status.Contains("disconnected"))
                    statusText.color = disconnectedColor;
                else
                    statusText.color = waitingColor;
            }
        }
        
        private void AddMessageToLog(string message)
        {
            if (messageLogText != null)
            {
                // Add timestamp and new message to the beginning of the log
                string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
                messageLogText.text = $"[{timestamp}] {message}\n" + messageLogText.text;
                
                // Limit log size (optional)
                if (messageLogText.text.Length > 5000)
                {
                    messageLogText.text = messageLogText.text.Substring(0, 5000);
                }
                
                // Scroll to beginning if a ScrollRect was found
                if (scrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    scrollRect.verticalNormalizedPosition = 1f; // Scroll all the way to the top
                }
            }
        }
        
        private void SendMessage()
        {
            UDPClient clientComponent = udpClient as UDPClient;
            if (messageInputField != null && clientComponent != null)
            {
                string message = messageInputField.text;
                if (!string.IsNullOrEmpty(message))
                {
                    clientComponent.SendNetworkMessage(message);
                    messageInputField.text = "";
                    
                    // Return focus to input field (optional)
                    messageInputField.ActivateInputField();
                }
            }
        }
        
        // Public methods to adjust font size and colors
        public void SetStatusFontSize(int newSize)
        {
            statusFontSize = newSize;
            if (statusText != null)
                statusText.fontSize = statusFontSize;
        }
        
        public void SetLogFontSize(int newSize)
        {
            messageLogFontSize = newSize;
            if (messageLogText != null)
                messageLogText.fontSize = messageLogFontSize;
        }
        
        public void SetConnectedColor(Color newColor)
        {
            connectedColor = newColor;
            // Refresh status text to apply color if status is "connected"
            if (statusText != null && statusText.text.Contains("Connected"))
                statusText.color = connectedColor;
        }
        
        public void SetDisconnectedColor(Color newColor)
        {
            disconnectedColor = newColor;
            // Refresh status text to apply color if status is "disconnected"
            if (statusText != null && statusText.text.Contains("disconnected"))
                statusText.color = disconnectedColor;
        }
        
        public void SetWaitingColor(Color newColor)
        {
            waitingColor = newColor;
            // Refresh status text to apply color if status is in waiting state
            if (statusText != null && !statusText.text.Contains("Connected") && !statusText.text.Contains("disconnected"))
                statusText.color = waitingColor;
        }
    }
}