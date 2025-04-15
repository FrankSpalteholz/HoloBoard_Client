using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Converts 2D hand tracking data to 3D space by estimating depth using hand size.
/// </summary>
public class HandPositionInterpolator : MonoBehaviour
{
    // Singleton pattern
    public static HandPositionInterpolator Instance { get; private set; }
    
    [Header("Visualization")]
    [SerializeField] private GameObject handVisualizerPrefab;
    [SerializeField] private bool showVisualizer = true;
    
    [Header("Depth Mapping")]
    [SerializeField] private float minDepth = 0.2f; // Minimum Z distance in meters
    [SerializeField] private float maxDepth = 1.0f; // Maximum Z distance in meters
    [SerializeField] private float minHandSize = 0.05f; // Size when hand is far
    [SerializeField] private float maxHandSize = 0.3f; // Size when hand is close
    
    [Header("Pointing Settings")]
    [SerializeField] private bool onlyTrackWhenPointing = true;
    [SerializeField] private float pointerSmoothingFactor = 5f; // Higher = more smoothing
    [SerializeField] private bool stickyMode = true; // When true, visualizer sticks directly to finger with no smoothing
    [SerializeField] private bool use2DTrackingOnly = false; // When true, only 2D position is used (Z stays constant)
    
    [Header("2D Mode Settings")]
    [SerializeField] private float fixedDepth = 0.5f; // Fixed Z distance when using 2D tracking
    [SerializeField] private float fixedHandSize = 0.15f; // Fixed hand size when using 2D tracking
    
    [Header("Mapping")]
    [SerializeField] private Vector2 screenMappingOffset = new Vector2(0.5f, 0.5f); // Center of screen
    [SerializeField] private float horizontalScale = 1.0f; // Horizontal mapping scale
    [SerializeField] private float verticalScale = 1.0f; // Vertical mapping scale
    [SerializeField] private bool invertXAxis = true; // Invert X-axis (left/right)
    [SerializeField] private bool invertYAxis = true; // Invert Y-axis (up/down)
    
    // Runtime references
    private GameObject visualizerInstance;
    private Vector3 targetPosition = Vector3.zero;
    private Vector3 currentPosition = Vector3.zero;
    private bool isHandVisible = false;
    private bool isPointing = false;
    
    // Calculated values
    private float estimatedDepth = 0.5f;
    private float handSizeNormalized = 0;
    private Camera mainCam;
    
    // Debug
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        // Create visualizer if assigned
        if (handVisualizerPrefab != null && showVisualizer)
        {
            visualizerInstance = Instantiate(handVisualizerPrefab);
            visualizerInstance.SetActive(false);
        }
        
        // Get main camera reference
        mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("Main camera not found. Using default projection settings.");
        }
    }
    
    private void Update()
    {
        // Only update position if hand is visible and (pointing or tracking all gestures)
        if (isHandVisible && (!onlyTrackWhenPointing || isPointing))
        {
            if (stickyMode)
            {
                // In sticky mode, directly use target position with no smoothing
                currentPosition = targetPosition;
            }
            else
            {
                // Use smoothing in normal mode
                currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * pointerSmoothingFactor);
            }
            
            // Update visualizer
            if (visualizerInstance != null && showVisualizer)
            {
                visualizerInstance.SetActive(true);
                visualizerInstance.transform.position = currentPosition;
                
                // Optionally scale visualizer based on depth
                float scale = Mathf.Lerp(0.5f, 1.0f, 1.0f - handSizeNormalized);
                visualizerInstance.transform.localScale = Vector3.one * scale;
            }
        }
        else if (visualizerInstance != null)
        {
            visualizerInstance.SetActive(false);
        }
    }
    
    /// <summary>
    /// Update hand tracking information with MediaPipe data
    /// </summary>
    /// <param name="normalizedHandPosition">Normalized hand position (0-1 in screen space)</param>
    /// <param name="palmSize">Size of palm in normalized units (0-1)</param>
    /// <param name="isPointingGesture">Whether the hand is making a pointing gesture</param>
    public void UpdateHandPosition(Vector2 normalizedHandPosition, float palmSize, bool isPointingGesture)
    {
        isHandVisible = true;
        isPointing = isPointingGesture;
        
        // In 2D tracking mode, use fixed values
        if (use2DTrackingOnly)
        {
            handSizeNormalized = fixedHandSize;
            estimatedDepth = fixedDepth;
        }
        else
        {
            // Normal 3D mode - calculate depth from hand size
            // Normalize palm size to 0-1 range for depth calculation
            handSizeNormalized = Mathf.Clamp01((palmSize - minHandSize) / (maxHandSize - minHandSize));
            
            // Calculate depth based on hand size (larger hand = closer = smaller depth)
            estimatedDepth = Mathf.Lerp(maxDepth, minDepth, handSizeNormalized);
        }
        
        // Map screen coordinates to world space
        Vector3 worldPosition = ScreenToWorldPoint(normalizedHandPosition, estimatedDepth);
        
        // Update target position
        targetPosition = worldPosition;
        
        if (showDebugInfo)
        {
            Debug.Log($"Hand position updated: Screen:{normalizedHandPosition}, " +
                     $"Palm size:{palmSize:F3}, Depth:{estimatedDepth:F2}, " + 
                     $"World:{worldPosition}, Pointing:{isPointingGesture}, 2D Mode:{use2DTrackingOnly}");
        }
    }
    
    /// <summary>
    /// Mark hand as not visible/detected
    /// </summary>
    public void ClearHandData()
    {
        isHandVisible = false;
        
        // Use MainThreadDispatcher to ensure SetActive is called on the main thread
        if (visualizerInstance != null)
        {
            NetworkFramework.MainThreadDispatcher.RunOnMainThread(() => {
                if (visualizerInstance != null) {
                    visualizerInstance.SetActive(false);
                }
            });
        }
    }
    
    /// <summary>
    /// Convert normalized screen position to world position with a given depth
    /// </summary>
    private Vector3 ScreenToWorldPoint(Vector2 normalizedScreenPos, float depth)
    {
        // Apply axis inversion if needed
        if (invertXAxis)
        {
            normalizedScreenPos.x = 1.0f - normalizedScreenPos.x;
        }
        
        if (invertYAxis)
        {
            normalizedScreenPos.y = 1.0f - normalizedScreenPos.y;
        }
        
        if (mainCam != null)
        {
            // Adjust normalized position with mapping offset and scale
            float adjustedX = (normalizedScreenPos.x - screenMappingOffset.x) * horizontalScale;
            float adjustedY = (normalizedScreenPos.y - screenMappingOffset.y) * verticalScale;
            
            // Convert to viewport position (0-1)
            Vector3 viewportPoint = new Vector3(adjustedX + 0.5f, adjustedY + 0.5f, depth);
            
            // Convert to world position using the camera's viewport to world transformation
            return mainCam.ViewportToWorldPoint(viewportPoint);
        }
        else
        {
            // Fallback if no camera: simple 3D mapping
            float adjustedX = (normalizedScreenPos.x - screenMappingOffset.x) * horizontalScale;
            float adjustedY = (normalizedScreenPos.y - screenMappingOffset.y) * verticalScale;
            
            return new Vector3(adjustedX, adjustedY, -depth); // Negative depth to go into the screen
        }
    }
    
    /// <summary>
    /// Get current 3D position 
    /// </summary>
    public Vector3 GetCurrentPosition()
    {
        return currentPosition;
    }
    
    /// <summary>
    /// Get current estimated depth
    /// </summary>
    public float GetEstimatedDepth()
    {
        return estimatedDepth;
    }
    
    /// <summary>
    /// Check if hand is currently visible
    /// </summary>
    public bool IsHandVisible()
    {
        return isHandVisible;
    }
    
    /// <summary>
    /// Check if hand is currently pointing
    /// </summary>
    public bool IsPointing()
    {
        return isPointing;
    }
    
    /// <summary>
    /// Toggle 2D tracking mode
    /// </summary>
    public void Toggle2DTrackingMode()
    {
        use2DTrackingOnly = !use2DTrackingOnly;
        
        if (showDebugInfo)
        {
            Debug.Log($"2D Tracking mode {(use2DTrackingOnly ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Set 2D tracking mode
    /// </summary>
    public void Set2DTrackingMode(bool enable)
    {
        use2DTrackingOnly = enable;
    }
    
    /// <summary>
    /// Set visualization prefab at runtime
    /// </summary>
    public void SetVisualizerPrefab(GameObject prefab)
    {
        // Clean up existing visualizer
        if (visualizerInstance != null)
        {
            Destroy(visualizerInstance);
            visualizerInstance = null;
        }
        
        // Set new prefab and instantiate if needed
        handVisualizerPrefab = prefab;
        
        if (handVisualizerPrefab != null && showVisualizer)
        {
            visualizerInstance = Instantiate(handVisualizerPrefab);
            visualizerInstance.SetActive(isHandVisible && (!onlyTrackWhenPointing || isPointing));
            
            if (visualizerInstance.activeSelf)
            {
                visualizerInstance.transform.position = currentPosition;
            }
        }
    }
    
    /// <summary>
    /// Toggle sticky mode
    /// </summary>
    public void ToggleStickyMode()
    {
        stickyMode = !stickyMode;
        
        if (showDebugInfo)
        {
            Debug.Log($"Sticky mode {(stickyMode ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Set sticky mode
    /// </summary>
    public void SetStickyMode(bool sticky)
    {
        stickyMode = sticky;
    }
    
    /// <summary>
    /// Toggle visualizer visibility
    /// </summary>
    public void ToggleVisualizer()
    {
        showVisualizer = !showVisualizer;
        
        if (visualizerInstance != null)
        {
            visualizerInstance.SetActive(showVisualizer && isHandVisible && (!onlyTrackWhenPointing || isPointing));
        }
        else if (showVisualizer && handVisualizerPrefab != null)
        {
            // Create visualizer if needed
            visualizerInstance = Instantiate(handVisualizerPrefab);
            visualizerInstance.SetActive(isHandVisible && (!onlyTrackWhenPointing || isPointing));
        }
    }
}