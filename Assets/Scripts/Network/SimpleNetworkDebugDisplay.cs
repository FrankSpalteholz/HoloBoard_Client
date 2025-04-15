using UnityEngine;
using TMPro;
using NetworkFramework;
using System.Collections;

/// <summary>
/// Simple network debug display for the DebugManager.
/// Shows minimal network status information.
/// </summary>
public class SimpleNetworkDebugDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI networkStatusText;
    
    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.5f; // How often to update (seconds)
    
    [Header("Colors")]
    [SerializeField] private Color connectedColor = new Color(0, 0.5f, 1f); // Ocean blue
    [SerializeField] private Color disconnectedColor = new Color(1f, 0.5f, 0); // Orange
    
    // Network reference
    private UDPClient udpClient;
    
    private void Start()
    {
        // Find UDPClient if not assigned
        udpClient = UDPClient.Instance;
            
        // Initialize UI text
        if (networkStatusText != null)
        {
            networkStatusText.text = "Network: Initializing...";
            networkStatusText.color = disconnectedColor;
        }
        
        // Start regular update routine
        StartCoroutine(UpdateRoutine());
    }
    
    private IEnumerator UpdateRoutine()
    {
        WaitForSeconds waitTime = new WaitForSeconds(updateInterval);
        
        while (true)
        {
            if (networkStatusText != null && udpClient != null)
            {
                UpdateNetworkStatus();
            }
            
            yield return waitTime;
        }
    }
    
    private void UpdateNetworkStatus()
    {
        string connectionStatus = udpClient.connectionStatus;
        bool isConnected = connectionStatus.Contains("Connected");
        
        // Set color based on connection status
        networkStatusText.color = isConnected ? connectedColor : disconnectedColor;
        
        // Update text with server info
        networkStatusText.text = $"<b>Network Status</b>\nServer: {udpClient.serverIP} - {(isConnected ? "ONLINE" : "OFFLINE")}";
    }
}