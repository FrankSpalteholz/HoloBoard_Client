using UnityEngine;
using TMPro;

public class GazeScreenIntersection : MonoBehaviour
{
    [Header("Tracking-Referenzen")]
    [SerializeField] private Transform headTrackingTransform; // Die Transform für Kopf-Tracking
    [SerializeField] private Transform phoneScreen; // Eine Transform, die das iPhone-Display repräsentiert
    [SerializeField] private Camera arCamera; // AR-Kamera-Referenz für Koordinatentransformation

    [Header("Physische Display-Eigenschaften")]
    [SerializeField] private float screenWidthCm = 14.24f; // Physische Breite des iPhone 12 Pro Displays in cm
    [SerializeField] private float screenHeightCm = 6.59f; // Physische Höhe des iPhone 12 Pro Displays in cm
    [SerializeField] private Vector2 screenResolution = new Vector2(2532, 1170); // Auflösung des Displays
    
    [Header("UI-Referenzen")]
    [SerializeField] private RectTransform gazeMarker; // Marker für den Kopfpositions-Projektion
    [SerializeField] private RectTransform contentPanel; // Das 16:9 ContentPanel
    [SerializeField] private TextMeshProUGUI debugInfoText; // TextMeshPro für Debug-Informationen
    
    [Header("Einstellungen")]
    [SerializeField] private Vector3 faceCamOffset = new Vector3(0, 0, 0); // Offset der FaceTracking-Kamera zum Display
    [SerializeField] private float movementScaleFactor = 5.0f; // Skalierungsfaktor für stärkere Bewegung
    [SerializeField] private bool invertXAxis = true; // X-Achse umkehren
    [SerializeField] private bool restrictToPanel = true; // Marker nur im Panel anzeigen
    [SerializeField] private bool showDebugInfo = true;
    
    // Letzter berechneter Punkt (normalisiert 0-1)
    private Vector2 lastPositionNormalized = new Vector2(0.5f, 0.5f);
    
    void Start()
    {
        // Stelle sicher, dass alle benötigten Komponenten vorhanden sind
        if (headTrackingTransform == null)
        {
            Debug.LogError("Head Tracking Transform nicht zugewiesen!");
            enabled = false;
            return;
        }
        
        if (phoneScreen == null)
        {
            Debug.LogWarning("Phone Screen Transform nicht zugewiesen! Verwende die Transform dieses GameObjects.");
            phoneScreen = transform;
        }
        
        if (gazeMarker == null)
        {
            Debug.LogWarning("Gaze Marker nicht zugewiesen! Position wird nicht visuell dargestellt.");
        }
        
        // Suche AR-Kamera, falls nicht zugewiesen
        if (arCamera == null)
        {
            arCamera = Camera.main;
            if (arCamera == null)
            {
                Debug.LogWarning("Keine AR-Kamera gefunden oder zugewiesen! Koordinatentransformation könnte ungenau sein.");
            }
        }
        
        // Initialisierung des Debug-Texts
        if (debugInfoText != null && showDebugInfo)
        {
            debugInfoText.text = "Head Position Tracking initialisiert";
        }
    }
    
    void Update()
    {
        CalculateHeadPositionRelativeToScreen();
    }
    
    private void CalculateHeadPositionRelativeToScreen()
    {
        // 1. Aktuelle Kopfposition mit Offset
        Vector3 currentHeadPosition = headTrackingTransform.position;
        
        // 2. Berechne den relativen Offset zwischen Kopf und Bildschirmmitte im Weltkoordinatensystem
        Vector3 screenCenter = phoneScreen.position;
        Vector3 headToScreenOffset = currentHeadPosition - screenCenter;
        
        // 3. Transformiere diesen Offset in das lokale Koordinatensystem des Bildschirms
        Vector3 localOffset = phoneScreen.InverseTransformDirection(headToScreenOffset);
        
        // 4. Addiere den manuellen FaceCam-Offset (im lokalen Raum)
        localOffset += faceCamOffset;
        
        // 5. Wende den Skalierungsfaktor an, um die Bewegung zu verstärken
        localOffset *= movementScaleFactor;
        
        // 6. Normalisiere die X/Y-Komponenten relativ zur Bildschirmgröße
        float normalizedX;
        
        // X-Achse umkehren, wenn invertXAxis aktiviert ist
        if (invertXAxis)
        {
            normalizedX = localOffset.x / (screenWidthCm * 0.5f); // Jetzt wird links zu links und rechts zu rechts
        }
        else
        {
            normalizedX = -localOffset.x / (screenWidthCm * 0.5f); // Original: negativ, weil Bildschirm-X und Head-X gegenläufig sind
        }
        
        float normalizedY = localOffset.y / (screenHeightCm * 0.5f);
        
        // 7. Beginne in der Mitte und addiere die Bewegung
        normalizedX = 0.5f + normalizedX;
        normalizedY = 0.5f + normalizedY;
        
        // 8. Beschränke auf den gültigen Bereich, wenn gewünscht
        bool isInsideScreen = true;
        
        if (restrictToPanel)
        {
            // Beschränke auf den Bereich 0 bis 1
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);
        }
        else
        {
            // Prüfe, ob der Punkt außerhalb des Bildschirms liegt
            if (normalizedX < 0 || normalizedX > 1 || normalizedY < 0 || normalizedY > 1)
            {
                isInsideScreen = false;
                if (debugInfoText != null && showDebugInfo)
                {
                    debugInfoText.text = "Projektion außerhalb des Bildschirms";
                }
            }
        }
        
        // 9. Speichere den normalisierten Punkt
        lastPositionNormalized = new Vector2(normalizedX, normalizedY);
        
        // 10. Aktualisiere die UI-Position des Markers
        if (isInsideScreen || restrictToPanel)
        {
            UpdatePositionMarker(normalizedX, normalizedY);
            
            // 11. Debug-Info aktualisieren
            if (debugInfoText != null && showDebugInfo)
            {
                debugInfoText.text = string.Format(
                    "Kopfposition:\nNormalisiert: ({0:F2}, {1:F2})\nOffset: ({2:F2}, {3:F2}, {4:F2})",
                    lastPositionNormalized.x, lastPositionNormalized.y,
                    localOffset.x, localOffset.y, localOffset.z
                );
            }
        }
    }
    
    private void UpdatePositionMarker(float normalizedX, float normalizedY)
    {
        if (gazeMarker == null || contentPanel == null) return;
        
        // Berechne die Position im ContentPanel (16:9-Verhältnis)
        float contentPanelWidth = contentPanel.rect.width;
        float contentPanelHeight = contentPanel.rect.height;
        
        // Setze die Position des Markers (normalisierte Koordinaten zu Panel-Koordinaten)
        gazeMarker.anchoredPosition = new Vector2(
            normalizedX * contentPanelWidth - contentPanelWidth / 2,
            normalizedY * contentPanelHeight - contentPanelHeight / 2
        );
    }
    
    // Öffentliche Methode, um den letzten berechneten Punkt zu erhalten (0-1 normalisiert)
    public Vector2 GetHeadPositionProjection()
    {
        return lastPositionNormalized;
    }
    
    // Öffentliche Methode, um zu prüfen, ob ein gültiger Punkt vorhanden ist
    public bool HasValidProjection()
    {
        // Werte außerhalb von 0-1 würden auf eine ungültige Projektion hindeuten
        return lastPositionNormalized.x >= 0 && lastPositionNormalized.x <= 1 &&
               lastPositionNormalized.y >= 0 && lastPositionNormalized.y <= 1;
    }
    
    // Diese Methode kann später für die Kommunikation mit dem Mac verwendet werden
    public void SendPositionToServer()
    {
        if (!HasValidProjection()) return;
        
        // In der späteren Implementierung:
        // 1. Hole Referenz zum UDPClient
        // 2. Formatiere die Positions-Daten
        // 3. Sende die Daten über das Netzwerk
    }
    
    // Berechnet die aktuelle Z-Distanz zwischen Kopf und Bildschirm in cm
    public float GetZDistance()
    {
        if (headTrackingTransform == null || phoneScreen == null)
            return 0f;
            
        Vector3 headPosition = headTrackingTransform.position;
        Vector3 screenPosition = phoneScreen.position;
        Vector3 screenNormal = -phoneScreen.forward;
        
        // Vektor vom Kopf zum Bildschirm
        Vector3 headToScreen = screenPosition - headPosition;
        
        // Distanz des Kopfs zur Bildschirmebene
        float distance = Vector3.Dot(headToScreen, screenNormal);
        
        // In cm umrechnen (Unity-Einheiten sind standardmäßig Meter)
        return distance * 100f;
    }
    
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, Screen.height - 320, 300, 310));
        GUILayout.Label("Tracking-Einstellungen");
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("X-Offset: " + faceCamOffset.x.ToString("F2") + " cm");
        if (GUILayout.Button("-0.5")) faceCamOffset.x -= 0.5f;
        if (GUILayout.Button("+0.5")) faceCamOffset.x += 0.5f;
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Y-Offset: " + faceCamOffset.y.ToString("F2") + " cm");
        if (GUILayout.Button("-0.5")) faceCamOffset.y -= 0.5f;
        if (GUILayout.Button("+0.5")) faceCamOffset.y += 0.5f;
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Z-Offset: " + faceCamOffset.z.ToString("F2") + " cm");
        if (GUILayout.Button("-0.2")) faceCamOffset.z -= 0.2f;
        if (GUILayout.Button("+0.2")) faceCamOffset.z += 0.2f;
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Skalierung: " + movementScaleFactor.ToString("F1"));
        if (GUILayout.Button("-1.0")) movementScaleFactor = Mathf.Max(1.0f, movementScaleFactor - 1.0f);
        if (GUILayout.Button("+1.0")) movementScaleFactor += 1.0f;
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        bool currentInvertX = invertXAxis;
        invertXAxis = GUILayout.Toggle(invertXAxis, "X-Achse umkehren");
        if (currentInvertX != invertXAxis)
        {
            Debug.Log("X-Achse umkehren: " + invertXAxis);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndArea();
    }
}