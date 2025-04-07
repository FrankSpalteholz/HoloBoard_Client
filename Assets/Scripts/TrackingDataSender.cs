using UnityEngine;
using NetworkFramework;
using TMPro;

public class TrackingDataSender : MonoBehaviour
{
    [Header("Komponentenreferenzen")]
    [SerializeField] private GazeScreenIntersection gazeTracker;
    [SerializeField] private AudioDetector audioDetector;
    [SerializeField] private MonoBehaviour udpClientComponent; // Zieht das UDPClient-Skript
    
    [Header("Debug-Anzeige")]
    [SerializeField] private TextMeshProUGUI outputDebugText; // Anzeige der gesendeten Daten
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Einstellungen")]
    [SerializeField] private float sendRate = 0.05f; // Sendet 20 Mal pro Sekunde
    [SerializeField] private bool autoStartSending = false;
    
    [Header("Z-Distanz Einstellungen")]
    [SerializeField] private Transform headTrackingTransform;
    [SerializeField] private Transform phoneScreenTransform;
    [SerializeField] private Vector3 faceCamOffset = Vector3.zero;
    
    // Private Variablen
    private UDPClient udpClient;
    private float lastSendTime = 0f;
    private bool isSending = false;
    private string lastSentData = "";
    
    // Daten-Caching
    private Vector2 lastGazePosition;
    private float lastZDistance;
    private bool lastAudioTriggered;
    
    void Start()
    {
        // UDPClient-Referenz holen
        udpClient = udpClientComponent as UDPClient;
        if (udpClient == null)
        {
            Debug.LogError("UDPClient konnte nicht gefunden werden!");
            enabled = false;
            return;
        }
        
        // Prüfe, ob alle benötigten Komponenten vorhanden sind
        if (gazeTracker == null)
        {
            Debug.LogError("GazeTracker nicht zugewiesen!");
            enabled = false;
            return;
        }
        
        // Debug-Text initialisieren
        if (outputDebugText != null && showDebugInfo)
        {
            outputDebugText.text = "Warte auf Tracking-Daten...";
        }
        
        // Automatisch mit dem Senden beginnen, falls eingestellt
        if (autoStartSending)
        {
            StartSending();
        }
    }
    
    void Update()
    {
        if (!isSending) return;
        
        // Aktualisiere die Debug-Anzeige auch zwischen den Sendungen
        if (outputDebugText != null && showDebugInfo)
        {
            UpdateDebugDisplay();
        }
        
        // Prüfe, ob es Zeit ist, neue Daten zu senden
        if (Time.time - lastSendTime > sendRate)
        {
            SendTrackingData();
            lastSendTime = Time.time;
        }
    }
    
    private void SendTrackingData()
    {
        // Prüfe, ob die Netzwerkverbindung aktiv ist
        if (udpClient == null) return;
        
        // Hole die Gaze-Positionsdaten
        lastGazePosition = gazeTracker.GetHeadPositionProjection();
        
        // Berechne die Z-Distanz zwischen Kopf und Bildschirm
        lastZDistance = gazeTracker.GetZDistance();
        
        // Hole den Audio-Trigger-Status
        lastAudioTriggered = (audioDetector != null) && audioDetector.IsAudioTriggered();
        
        // Formatiere die Daten
        lastSentData = string.Format("DATA:{0:F4},{1:F4},{2:F2},{3}",
            lastGazePosition.x, lastGazePosition.y, lastZDistance, lastAudioTriggered ? 1 : 0);
        
        // Sende die Daten
        udpClient.SendNetworkMessage(lastSentData);
        
        // Debug-Ausgabe
        Debug.Log("Tracking-Daten gesendet: " + lastSentData);
        
        // Debug-Anzeige aktualisieren
        UpdateDebugDisplay();
    }
    
    private void UpdateDebugDisplay()
    {
        if (outputDebugText == null || !showDebugInfo) return;
        
        string audioStatus = lastAudioTriggered ? "<color=red>AKTIV</color>" : "inaktiv";
        string connectionStatus = udpClient.connectionStatus;
        string sendRateInfo = (sendRate * 1000).ToString("F0") + " ms";
        
        outputDebugText.text = string.Format(
            "<b>TRACKING DATEN</b>\n\n" +
            "Position X: <b>{0:F2}</b>\n" +
            "Position Y: <b>{1:F2}</b>\n" +
            "Distanz Z: <b>{2:F1} cm</b>\n" +
            "Audio-Trigger: <b>{3}</b>\n\n" +
            "<size=80%>Senderate: {4}\n" +
            "Netzwerk: {5}\n" +
            "Datenpaket: {6}</size>",
            lastGazePosition.x, lastGazePosition.y, lastZDistance, 
            audioStatus, sendRateInfo, connectionStatus, lastSentData
        );
    }
    
    // Öffentliche Methoden zum Steuern des Sendens
    
    public void StartSending()
    {
        isSending = true;
        lastSendTime = Time.time;
        Debug.Log("Tracking-Datenübertragung gestartet");
        
        if (outputDebugText != null && showDebugInfo)
        {
            outputDebugText.text = "Tracking-Datenübertragung gestartet...";
        }
    }
    
    public void StopSending()
    {
        isSending = false;
        Debug.Log("Tracking-Datenübertragung gestoppt");
        
        if (outputDebugText != null && showDebugInfo)
        {
            outputDebugText.text = "Tracking-Datenübertragung gestoppt.";
        }
    }
    
    public void ToggleSending()
    {
        if (isSending)
            StopSending();
        else
            StartSending();
    }
}