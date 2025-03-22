using UnityEngine;
using NetworkFramework;
using TMPro;

public class TrackingDataSender : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private GazeScreenIntersection gazeTracker;
    [SerializeField] private AudioDetector audioDetector;
    [SerializeField] private MonoBehaviour udpClientComponent; // Zieht das UDPClient-Skript
    
    [Header("Debug-Output")]
    [SerializeField] private TextMeshProUGUI outputDebugText; // Anzeige der gesendeten Daten
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Settings")]
    [SerializeField] private float sendRate = 0.05f; // Sendet 20 Mal pro Sekunde
    [SerializeField] private bool autoStartSending = false;
    [SerializeField] private bool enableSmoothing = true;
    [SerializeField] private int sampleCount = 3;
    
    [Header("Transform Settings")]
    [SerializeField] private Transform headTrackingTransform;
    [SerializeField] private Transform phoneScreenTransform;
    [SerializeField] private Vector3 faceCamOffset = Vector3.zero;
    
    // Private Variablen
    private UDPClient udpClient;
    private float lastSendTime = 0f;
    private bool isSending = false;
    private string lastSentData = "";

    private SimpleDataSmoother dataSmoother;
    
    // Daten-Caching
    private Vector2 lastGazePosition;
    private Vector3 lastHeadPosition;
    private Quaternion lastHeadRotation;
    private bool lastAudioTriggered;
    
    void Start()
    {
        // UDPClient-Referenz holen
        udpClient = udpClientComponent as UDPClient;
        if (udpClient == null)
        {
            Debug.LogError("UDPClient couldn't be found!");
            enabled = false;
            return;
        }
        
        // Prüfe, ob alle benötigten Komponenten vorhanden sind
        if (gazeTracker == null)
        {
            Debug.LogError("GazeTracker not applied!");
            enabled = false;
            return;
        }
        
        // Debug-Text initialisieren
        if (outputDebugText != null && showDebugInfo)
        {
            outputDebugText.text = "Waiting for Tracking-Data...";
        }
        
        // Automatisch mit dem Senden beginnen, falls eingestellt
        if (autoStartSending)
        {
            StartSending();
        }

        dataSmoother = new SimpleDataSmoother(sampleCount); // 3 Samples für das iPhone
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
            SendTrackingData(enableSmoothing);
            lastSendTime = Time.time;
        }
    }
    
    private void SendTrackingData(bool enableSmoothing)
    {

        if(enableSmoothing)
        {
            lastGazePosition = dataSmoother.SmoothGazePosition(gazeTracker.GetHeadPositionProjection());
            lastHeadPosition = dataSmoother.SmoothHeadPosition(headTrackingTransform.transform.position * 100);
            lastHeadRotation = dataSmoother.SmoothHeadRotation(headTrackingTransform.transform.rotation);
        }

        // Prüfe, ob die Netzwerkverbindung aktiv ist
        if (udpClient == null) return;
        
        // Hole die Gaze-Positionsdaten
        lastGazePosition = gazeTracker.GetHeadPositionProjection();

        lastHeadPosition = headTrackingTransform.transform.position * 100;

        lastHeadRotation = headTrackingTransform.transform.rotation;
        
        // Hole den Audio-Trigger-Status
        lastAudioTriggered = (audioDetector != null) && audioDetector.IsAudioTriggered();
        
        // Formatiere die Daten
        lastSentData = string.Format("DATA:{0:F4},{1:F4},{2:F2},{3:F2},{4:F2},{5:F4},{6:F4},{7:F4},{8:F4},{9}",
            lastGazePosition.x, 
            lastGazePosition.y, 
            lastHeadPosition.x, 
            lastHeadPosition.y, 
            lastHeadPosition.z,
            lastHeadRotation.x,
            lastHeadRotation.y,
            lastHeadRotation.z,
            lastHeadRotation.w, 
            lastAudioTriggered ? 1 : 0);
        
        // Sende die Daten
        udpClient.SendNetworkMessage(lastSentData);
        
        // Debug-Ausgabe
        Debug.Log("Tracking-Data send: " + lastSentData);
        
        // Debug-Anzeige aktualisieren
        UpdateDebugDisplay();
    }
    
    private void UpdateDebugDisplay()
    {
        if (outputDebugText == null || !showDebugInfo) return;
        
        string audioStatus = lastAudioTriggered ? "<color=red>AKTIV</color>" : "inactive";
        string connectionStatus = udpClient.connectionStatus;
        string sendRateInfo = (sendRate * 1000).ToString("F0") + " ms";
        
        outputDebugText.text = string.Format(
            "<b>TRACKING DATA</b>\n\n" +
            "Position X: <b>{0:F2}</b>\n" +
            "Position Y: <b>{1:F2}</b>\n" +
            "Distance Z: <b>{2:F1} cm</b>\n" +
            "Audio-Trigger: <b>{3}</b>\n\n" +
            "<size=80%>Sendrate: {4}\n" +
            "Network: {5}\n" +
            "Datapakets: {6}</size>",
            lastGazePosition.x, lastGazePosition.y, lastHeadPosition.z, 
            audioStatus, sendRateInfo, connectionStatus, lastSentData
        );
    }
    
    // Öffentliche Methoden zum Steuern des Sendens
    
    public void StartSending()
    {
        isSending = true;
        lastSendTime = Time.time;
        Debug.Log("Tracking-Data send started");
        
        if (outputDebugText != null && showDebugInfo)
        {
            outputDebugText.text = "Tracking-Data send started ...";
        }
    }
    
    public void StopSending()
    {
        isSending = false;
        Debug.Log("Tracking-Data send stopped");
        
        if (outputDebugText != null && showDebugInfo)
        {
            outputDebugText.text = "Tracking-Data send stopped.";
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