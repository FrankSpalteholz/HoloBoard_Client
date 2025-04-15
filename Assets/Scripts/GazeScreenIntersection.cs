using UnityEngine;
using TMPro;

public class GazeScreenIntersection : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform gazeMarker; // Marker for head position projection
    [SerializeField] private RectTransform contentPanel; // The 16:9 content panel
    [SerializeField] private TextMeshProUGUI debugInfoText; // TextMeshPro for debug information
    
    [Header("Settings")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Reference to the tracker manager
    private GazeTrackerManager trackerManager;
    
    // Last known gaze position
    private Vector2 lastGazePosition = new Vector2(0.5f, 0.5f);
    
    void Start()
    {
        // Look for GazeTrackerManager
        trackerManager = GazeTrackerManager.Instance;
        
        if (trackerManager == null)
        {
            Debug.LogError("GazeTrackerManager not found! Please add GazeTrackerManager to your scene.");
            enabled = false;
            return;
        }
        
        // Register for gaze position updates
        trackerManager.OnGazePositionUpdated += OnGazePositionUpdated;
        
        // Ensure all needed components are present
        if (gazeMarker == null)
        {
            Debug.LogWarning("Gaze Marker not assigned! Position will not be visually displayed.");
        }
        
        if (contentPanel == null)
        {
            Debug.LogWarning("Content Panel not assigned! Using screen dimensions.");
        }
        
        // Initialize debug text
        if (debugInfoText != null && showDebugInfo)
        {
            debugInfoText.text = "Head Position Tracking initialized";
        }
    }
    
    void OnDestroy()
    {
        // Unregister from events
        if (trackerManager != null)
        {
            trackerManager.OnGazePositionUpdated -= OnGazePositionUpdated;
        }
    }
    
    // Event handler for gaze position updates
    private void OnGazePositionUpdated(Vector2 normalizedPosition)
    {
        // Save latest position
        lastGazePosition = normalizedPosition;
        
        // Update UI position
        UpdatePositionMarker(normalizedPosition.x, normalizedPosition.y);
        
        // Update debug info
        if (debugInfoText != null && showDebugInfo)
        {
            float zDistance = trackerManager.GetZDistance();
            
            debugInfoText.text = string.Format(
                "Head position:\n" +
                "Normalized: ({0:F2}, {1:F2})\n" +
                "Z Distance: {2:F1} cm",
                lastGazePosition.x, lastGazePosition.y, zDistance
            );
        }
    }
    
    private void UpdatePositionMarker(float normalizedX, float normalizedY)
    {
        if (gazeMarker == null || contentPanel == null) return;
        
        // Calculate position in ContentPanel (16:9 ratio)
        float contentPanelWidth = contentPanel.rect.width;
        float contentPanelHeight = contentPanel.rect.height;
        
        // Set the marker position (normalized coordinates to panel coordinates)
        gazeMarker.anchoredPosition = new Vector2(
            normalizedX * contentPanelWidth - contentPanelWidth / 2,
            normalizedY * contentPanelHeight - contentPanelHeight / 2
        );
    }
    
    // Public method to get the current gaze position (0-1 normalized)
    public Vector2 GetHeadPositionProjection()
    {
        return lastGazePosition;
    }
    
    // Public method to check if a valid position is available
    public bool HasValidProjection()
    {
        return trackerManager != null && trackerManager.HasValidProjection();
    }
    
    // Public method to get Z distance
    public float GetZDistance()
    {
        return trackerManager != null ? trackerManager.GetZDistance() : 0f;
    }
}