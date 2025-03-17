using UnityEngine;
using TMPro;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Text;

public class DeviceInfoDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI deviceInfoText;
    
    private void Start()
    {
        UpdateDeviceInfo();
    }
    
    public void UpdateDeviceInfo()
    {
        if (deviceInfoText == null) return;
        
        string deviceType = Application.platform.ToString();
        string deviceModel = SystemInfo.deviceModel;
        string ipAddresses = GetLocalIPAddresses();
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Gerät: {deviceType}");
        sb.AppendLine($"Modell: {deviceModel}");
        sb.AppendLine("IP-Adressen:");
        sb.Append(ipAddresses);
        
        deviceInfoText.text = sb.ToString();
    }
    
    private string GetLocalIPAddresses()
    {
        StringBuilder result = new StringBuilder();
        List<string> ipList = new List<string>();
        
        // Alle Netzwerkschnittstellen durchlaufen
        try 
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Nur aktive Schnittstellen betrachten
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                
                // Nur typische Netzwerktypen für Verbindungen betrachten
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || 
                    ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        // Nur IPv4-Adressen verwenden
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipList.Add($"{ni.Name}: {ip.Address}");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Fehler beim Abrufen der IP-Adressen: " + e.Message);
            ipList.Add("Fehler beim Abrufen: " + e.Message);
        }
        
        // Falls keine Netzwerkschnittstellen gefunden wurden
        if (ipList.Count == 0)
        {
            ipList.Add("Keine aktiven Netzwerkschnittstellen gefunden");
            
            // Alternative Methode, um zumindest die lokale IP zu erhalten
            try
            {
                string hostName = Dns.GetHostName();
                IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
                foreach (IPAddress ip in hostEntry.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipList.Add($"Hostname-Methode: {ip}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Alternative IP-Methode fehlgeschlagen: " + e.Message);
            }
        }
        
        // Liste formatieren
        foreach (string ip in ipList)
        {
            result.AppendLine(ip);
        }
        
        return result.ToString();
    }
}