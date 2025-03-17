using UnityEngine;

public class PerspectiveWarpingController : MonoBehaviour
{
    public RenderTexture screenProjectionTexture;
    
    [Header("Bildschirm-Referenzen")]
    public Transform virtualScreen; // Der virtuelle Screen, der das "Fenster" in die 3D-Welt darstellt
    public Renderer virtualScreenRenderer; // Der Renderer des virtuellen Screens (für Bounds)
    public float physicalScreenWidth = 0.524f;  // 52.4 cm in Metern
    public float physicalScreenHeight = 0.24213f; // 24.213 cm in Metern
    
    [Header("Kamera-Referenzen")]
    public Transform viewerTransform; // Die Position des Betrachters (z.B. Kopf-Tracking)
    public Transform trackingCameraTransform; // Die Position der Tracking-Kamera (iPhone)
    
    [Header("Warping-Einstellungen")]
    [Range(0f, 1f)]
    public float warpStrength = 0.2f;
    
    [Header("Debug-Grid")]
    public bool showGrid = true;
    [Range(2, 50)]
    public int gridSize = 10;
    [Range(0.001f, 0.02f)]
    public float gridThickness = 0.006f;
    public Color gridColor = new Color(1, 1, 1, 0.5f);
    
    [Header("Debug-GUI")]
    public bool showDebugInfo = true;
    public Vector2 guiPosition = new Vector2(100, 66);
    public int fontSize = 24;
    
    private MeshRenderer meshRenderer;
    private GUIStyle guiStyle;
    private Vector3 currentViewerPosition;
    private Vector3 viewerRelativeToTrackingCam;
    
    // Berechnete Eigenschaften
    private float screenAspect;
    
    void Start()
    {
        // Hole den MeshRenderer
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer nicht gefunden!");
            enabled = false;
            return;
        }
        
        // Prüfe, ob der virtuelle Screen zugewiesen ist
        if (virtualScreen == null)
        {
            Debug.LogError("Virtueller Screen nicht zugewiesen!");
            enabled = false;
            return;
        }
        
        // Versuche den Renderer zu finden, falls nicht manuell zugewiesen
        if (virtualScreenRenderer == null)
        {
            virtualScreenRenderer = virtualScreen.GetComponent<Renderer>();
            if (virtualScreenRenderer == null)
            {
                Debug.LogError("Kein Renderer am virtuellen Screen gefunden!");
                enabled = false;
                return;
            }
        }
        
        // Berechne den Aspect Ratio des physischen Bildschirms
        screenAspect = physicalScreenWidth / physicalScreenHeight;
        
        // GUI-Style einrichten
        guiStyle = new GUIStyle();
        guiStyle.fontSize = fontSize;
        guiStyle.normal.textColor = Color.white;
        
        // Setup initial material parameters
        UpdateShaderParameters();
    }
    
    void Update()
    {
        // Aktualisiere die Betrachterposition
        UpdateViewerPosition();
        
        // Aktualisiere die Shader-Parameter
        UpdateShaderParameters();
    }
    
    void UpdateViewerPosition()
    {
        if (viewerTransform != null)
        {
            // Verwende die tatsächliche Betrachterposition aus dem Tracking
            currentViewerPosition = viewerTransform.position;
            
            // Berechne die relative Position des Betrachters zur Tracking-Kamera
            if (trackingCameraTransform != null)
            {
                viewerRelativeToTrackingCam = currentViewerPosition - trackingCameraTransform.position;
            }
        }
        else
        {
            Debug.LogWarning("Kein Viewer-Transform zugewiesen!");
        }
    }
    
    // Hilfsmethode zum Berechnen des nächsten Punktes auf dem Screen
    Vector3 GetScreenCenterFromBounds()
    {
        if (virtualScreenRenderer == null)
            return virtualScreen.position;
            
        return virtualScreenRenderer.bounds.center;
    }
    
    // Hilfsmethode zum Berechnen des Abstands zum Screen basierend auf den Bounds
    float CalculateDistanceToScreen()
    {
        if (virtualScreenRenderer == null || viewerTransform == null)
            return 0f;
            
        // Hole den Mittelpunkt des Screens aus den Bounds
        Vector3 screenCenter = virtualScreenRenderer.bounds.center;
        
        // Berechne die Normale des Screens (wir nehmen an, dass der Forward-Vektor die Normale ist)
        Vector3 screenNormal = virtualScreen.forward;
        
        // Berechne den Vektor vom Betrachter zum Screen-Mittelpunkt
        Vector3 viewerToScreen = screenCenter - viewerTransform.position;
        
        // Projiziere diesen Vektor auf die Normale des Screens, um den senkrechten Abstand zu erhalten
        float distance = Mathf.Abs(Vector3.Dot(viewerToScreen, screenNormal));
        
        return distance;
    }
    
    // Hilfsmethode zum Berechnen des Abstands zwischen Tracking-Kamera und Screen
    float CalculateTrackingCamToScreenDistance()
    {
        if (virtualScreenRenderer == null || trackingCameraTransform == null)
            return 0f;
            
        // Hole den Mittelpunkt des Screens aus den Bounds
        Vector3 screenCenter = virtualScreenRenderer.bounds.center;
        
        // Berechne die Normale des Screens (wir nehmen an, dass der Forward-Vektor die Normale ist)
        Vector3 screenNormal = virtualScreen.forward;
        
        // Berechne den Vektor von der Tracking-Kamera zum Screen-Mittelpunkt
        Vector3 camToScreen = screenCenter - trackingCameraTransform.position;
        
        // Projiziere diesen Vektor auf die Normale des Screens, um den senkrechten Abstand zu erhalten
        float distance = Mathf.Abs(Vector3.Dot(camToScreen, screenNormal));
        
        return distance;
    }
    
    // Hilfsmethode zum Berechnen des nächsten Punktes auf dem Screen
    Vector3 GetClosestPointOnScreen(Vector3 position)
    {
        if (virtualScreenRenderer == null)
            return virtualScreen.position;
            
        // Hole den Mittelpunkt des Screens aus den Bounds
        Vector3 screenCenter = virtualScreenRenderer.bounds.center;
        
        // Berechne die Normale des Screens
        Vector3 screenNormal = virtualScreen.forward;
        
        // Berechne den Vektor vom Betrachter zum Screen-Mittelpunkt
        Vector3 positionToScreen = screenCenter - position;
        
        // Projiziere diesen Vektor auf die Normale des Screens
        float distance = Vector3.Dot(positionToScreen, screenNormal);
        
        // Berechne den nächsten Punkt auf dem Screen
        Vector3 closestPoint = position + screenNormal * distance;
        
        return closestPoint;
    }
    
    void UpdateShaderParameters()
    {
        if (meshRenderer == null || meshRenderer.material == null)
            return;
            
        // Prüfe, ob virtualScreen gesetzt ist
        if (virtualScreen == null || virtualScreenRenderer == null)
        {
            Debug.LogError("Virtueller Screen ist nicht zugewiesen!");
            return;
        }
            
        // Setze die Textur
        if (screenProjectionTexture != null)
        {
            meshRenderer.material.SetTexture("_MainTex", screenProjectionTexture);
        }
        
        // Setze die Seitenverhältnis-Parameter
        meshRenderer.material.SetFloat("_ScreenAspect", screenAspect);
        meshRenderer.material.SetFloat("_TextureAspect", 1.0f); // Quadratische Textur
        
        // Setze die Betrachterposition
        meshRenderer.material.SetVector("_ViewerPosition", currentViewerPosition);
        
        // Setze die Tracking-Kamera-Position, falls vorhanden
        if (trackingCameraTransform != null)
        {
            meshRenderer.material.SetVector("_TrackingCamPosition", trackingCameraTransform.position);
            meshRenderer.material.SetVector("_ViewerRelativeToTrackingCam", viewerRelativeToTrackingCam);
        }
        
        // Setze die Bildschirmparameter (jetzt aus den Bounds)
        Vector3 screenCenter = GetScreenCenterFromBounds();
        meshRenderer.material.SetVector("_ScreenPosition", screenCenter);
        meshRenderer.material.SetVector("_ScreenSize", new Vector2(physicalScreenWidth, physicalScreenHeight));
        
        // Setze die Warping-Stärke
        meshRenderer.material.SetFloat("_WarpStrength", warpStrength);
        
        // Setze die Debug-Grid-Parameter
        meshRenderer.material.SetFloat("_ShowGrid", showGrid ? 1.0f : 0.0f);
        meshRenderer.material.SetFloat("_GridSize", gridSize);
        meshRenderer.material.SetFloat("_GridThickness", gridThickness);
        meshRenderer.material.SetColor("_GridColor", gridColor);
    }
    
    void OnGUI()
    {
        if (!showDebugInfo)
            return;
            
        // GUI-Style aktualisieren
        guiStyle.fontSize = fontSize;
        
        float lineHeight = fontSize + 4;
        float currentY = guiPosition.y;
        
        // Zeige die aktuelle Betrachterposition
        GUI.Label(new Rect(guiPosition.x, currentY, 400, lineHeight), 
            $"Betrachterposition: ({currentViewerPosition.x:F2}, {currentViewerPosition.y:F2}, {currentViewerPosition.z:F2})", guiStyle);
        currentY += lineHeight;
        
        // Zeige den korrekten Abstand zum virtuellen Bildschirm (senkrechte Distanz zur Ebene)
        float viewerDistance = CalculateDistanceToScreen();
        GUI.Label(new Rect(guiPosition.x, currentY, 400, lineHeight), 
            $"Abstand zum Bildschirm: {viewerDistance:F3} m", guiStyle);
        currentY += lineHeight;
        
        // Zeige den Vektor vom Betrachter zum Screen
        if (virtualScreenRenderer != null && viewerTransform != null)
        {
            Vector3 screenCenter = virtualScreenRenderer.bounds.center;
            Vector3 viewerToScreen = screenCenter - viewerTransform.position;
            
            GUI.Label(new Rect(guiPosition.x, currentY, 400, lineHeight), 
                $"Distanz X: {viewerToScreen.x:F3} m", guiStyle);
            currentY += lineHeight;
            
            GUI.Label(new Rect(guiPosition.x, currentY, 400, lineHeight), 
                $"Distanz Y: {viewerToScreen.y:F3} m", guiStyle);
            currentY += lineHeight;
            
            GUI.Label(new Rect(guiPosition.x, currentY, 400, lineHeight), 
                $"Distanz Z: {viewerToScreen.z:F3} m", guiStyle);
            currentY += lineHeight;
        }
        
        // Zeige Tracking-Kamera-Informationen
        if (trackingCameraTransform != null)
        {
            // Abstand der Tracking-Kamera zum Screen
            float trackingCamDistance = CalculateTrackingCamToScreenDistance();
            GUI.Label(new Rect(guiPosition.x, currentY, 400, lineHeight), 
                $"Tracking-Cam zu Screen: {trackingCamDistance:F3} m", guiStyle);
            currentY += lineHeight;
            
            // Relative Position des Betrachters zur Tracking-Kamera
            GUI.Label(new Rect(guiPosition.x, currentY, 400, lineHeight), 
                $"Betrachter rel. zu Tracking-Cam: ({viewerRelativeToTrackingCam.x:F2}, {viewerRelativeToTrackingCam.y:F2}, {viewerRelativeToTrackingCam.z:F2})", guiStyle);
            currentY += lineHeight;
        }
        
        // Zeige die Warping-Stärke
        GUI.Label(new Rect(guiPosition.x, currentY, 400, lineHeight), 
            $"Warping-Stärke: {warpStrength:F2}", guiStyle);
        currentY += lineHeight;
        
        // Zeige den Bildschirm-Aspect-Ratio
        GUI.Label(new Rect(guiPosition.x, currentY, 400, lineHeight), 
            $"Bildschirm-Aspect: {screenAspect:F2}", guiStyle);
    }
    
    // Zeichne Gizmos zur Visualisierung im Editor
    void OnDrawGizmos()
    {
        if (virtualScreen == null)
            return;
            
        // Versuche Renderer zu bekommen, falls nicht vorhanden
        Renderer screenRenderer = virtualScreenRenderer;
        if (screenRenderer == null)
            screenRenderer = virtualScreen.GetComponent<Renderer>();
            
        if (screenRenderer == null)
            return;
            
        // Hole den tatsächlichen Mittelpunkt des Screens aus den Bounds
        Vector3 screenCenter = screenRenderer.bounds.center;
        
        // Zeichne den virtuellen Bildschirm basierend auf seinen tatsächlichen Bounds
        Gizmos.color = Color.green;
        Gizmos.matrix = Matrix4x4.identity; // Verwende weltkoordinaten
        Bounds bounds = screenRenderer.bounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        
        // Zeichne die normale des Screens aus dem Mittelpunkt der Bounds
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(screenCenter, virtualScreen.forward * 0.2f);
        
        // Zeichne die Tracking-Kamera, falls vorhanden
        if (trackingCameraTransform != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(trackingCameraTransform.position, 0.025f);
            
            // Zeichne eine Linie von der Tracking-Kamera zum nächsten Punkt auf dem Screen
            Vector3 closestPointForCam = GetClosestPointOnScreen(trackingCameraTransform.position);
            Gizmos.DrawLine(trackingCameraTransform.position, closestPointForCam);
            
            // Zeichne eine Ebene die der Tracking-Kamera entspricht
            Vector3 up = trackingCameraTransform.up * 0.05f;
            Vector3 right = trackingCameraTransform.right * 0.05f;
            Vector3 camPos = trackingCameraTransform.position;
            
            Gizmos.DrawLine(camPos - up - right, camPos - up + right);
            Gizmos.DrawLine(camPos - up + right, camPos + up + right);
            Gizmos.DrawLine(camPos + up + right, camPos + up - right);
            Gizmos.DrawLine(camPos + up - right, camPos - up - right);
            
            // Zeichne die Blickrichtung der Tracking-Kamera
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(camPos, trackingCameraTransform.forward * 0.2f);
        }
        
        // Zeichne die Betrachterposition
        if (Application.isPlaying && viewerTransform != null)
        {
            // Aktualisiere die aktuelle Betrachterposition
            Vector3 viewerPos = viewerTransform.position;
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(viewerPos, 0.02f);
            
            // Zeichne eine direkte Linie vom Betrachter zum Mittelpunkt des Bildschirms
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(viewerPos, screenCenter);
            
            // Zeichne den kürzesten Weg zum Bildschirm (senkrecht zur Ebene)
            Vector3 screenNormal = virtualScreen.forward;
            Vector3 viewerToScreen = screenCenter - viewerPos;
            float distance = Vector3.Dot(viewerToScreen, screenNormal);
            Vector3 closestPoint = viewerPos + screenNormal * distance;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(viewerPos, closestPoint);
            Gizmos.DrawSphere(closestPoint, 0.01f);
            
            // Wenn auch die Tracking-Kamera vorhanden ist, visualisiere die relative Position
            if (trackingCameraTransform != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(viewerPos, trackingCameraTransform.position);
            }
        }
    }
}