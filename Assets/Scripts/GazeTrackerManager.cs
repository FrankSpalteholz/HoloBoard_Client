using UnityEngine;
using System;

public class GazeTrackerManager : MonoBehaviour
{
    // Singleton pattern implementation
    public static GazeTrackerManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private Camera arCamera;

    [Header("Physical Display Settings")]
    [SerializeField] private float screenWidthCm = 14.24f; // Physical width of iPhone 12 Pro display in cm
    [SerializeField] private float screenHeightCm = 6.59f; // Physical height of iPhone 12 Pro display in cm
    [SerializeField] private Vector2 screenResolution = new Vector2(2532, 1170); // Display resolution

    [Header("Settings")]
    [SerializeField] private Vector3 faceCamOffset = Vector3.zero; // Offset for the face tracking camera
    [SerializeField] private float movementScaleFactor = 5.0f; // Scaling factor for stronger movement
    [SerializeField] private bool invertXAxis = true; // Invert X-axis
    [SerializeField] private bool restrictToPanel = true; // Restrict marker to panel
    [SerializeField] private bool showDebugInfo = false;

    // Events
    public event Action<Vector2> OnGazePositionUpdated;

    // Last calculated point (normalized 0-1)
    private Vector2 lastPositionNormalized = new Vector2(0.5f, 0.5f);
    
    // Head tracking references
    private FaceTransformProvider faceProvider;
    private Transform headTransform;
    private bool headTrackingAvailable = false;

    private void Awake()
    {
        // Singleton Pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
    }

    private void Start()
    {
        // Look for AR camera if not assigned
        if (arCamera == null)
        {
            arCamera = Camera.main;
            if (arCamera == null)
            {
                Debug.LogWarning("No AR camera found or assigned! Coordinate transformation may be inaccurate.");
            }
        }
        
        // Try to find FaceTransformProvider
        FindHeadTracking();
    }
    
    private void Update()
    {
        // If we don't have a head transform reference yet, try to find it
        if (!headTrackingAvailable)
        {
            FindHeadTracking();
            
            // If still not available, return
            if (!headTrackingAvailable)
                return;
        }
        
        // If we have head tracking but face is not tracked, return
        if (faceProvider != null && !faceProvider.IsFaceTracked())
            return;
            
        // Calculate gaze position based on head transform
        CalculateHeadPositionRelativeToScreen();
    }
    
    private void FindHeadTracking()
    {
        // Try to find FaceTransformProvider
        if (faceProvider == null)
        {
            faceProvider = FindObjectOfType<FaceTransformProvider>();
            
            if (faceProvider != null)
            {
                headTrackingAvailable = true;
                
                if (showDebugInfo)
                    Debug.Log("Found FaceTransformProvider for head tracking");
                
                // Set up event handlers
                faceProvider.OnFaceDetected += OnHeadDetected;
                faceProvider.OnFaceLost += OnHeadLost;
            }
        }
    }
    
    private void OnHeadDetected()
    {
        if (showDebugInfo)
            Debug.Log("Head detected for gaze tracking");
    }
    
    private void OnHeadLost()
    {
        if (showDebugInfo)
            Debug.Log("Head lost for gaze tracking");
    }
    
    private void CalculateHeadPositionRelativeToScreen()
    {
        if (faceProvider == null)
            return;
            
        // 1. Get current head position
        Vector3 currentHeadPosition = faceProvider.GetFacePosition();
        
        // 2. Calculate the relative offset between head and screen center
        // Using Vector3.zero as the screen center (origin)
        Vector3 screenCenter = Vector3.zero;
        Vector3 headToScreenOffset = currentHeadPosition - screenCenter;
        
        // 3. Transform this offset to the local coordinate system
        // Since we're using world origin, just use the offset directly
        Vector3 localOffset = headToScreenOffset;
        
        // 4. Add the manual FaceCam offset
        localOffset += faceCamOffset;
        
        // 5. Apply scaling factor to enhance movement
        localOffset *= movementScaleFactor;
        
        // 6. Normalize X/Y components relative to screen size
        float normalizedX;
        
        // Invert X-axis if invertXAxis is enabled
        if (invertXAxis)
        {
            normalizedX = localOffset.x / (screenWidthCm * 0.5f); // Now left becomes left and right becomes right
        }
        else
        {
            normalizedX = -localOffset.x / (screenWidthCm * 0.5f); // Original: negative because screen-X and head-X are opposite
        }
        
        float normalizedY = localOffset.y / (screenHeightCm * 0.5f);
        
        // 7. Start in the middle and add movement
        normalizedX = 0.5f + normalizedX;
        normalizedY = 0.5f + normalizedY;
        
        // 8. Restrict to valid range if desired
        bool isInsideScreen = true;
        
        if (restrictToPanel)
        {
            // Restrict to the range 0 to 1
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);
        }
        else
        {
            // Check if the point is outside the screen
            if (normalizedX < 0 || normalizedX > 1 || normalizedY < 0 || normalizedY > 1)
            {
                isInsideScreen = false;
                if (showDebugInfo)
                    Debug.Log("Projection outside screen");
            }
        }
        
        // 9. Save the normalized point
        lastPositionNormalized = new Vector2(normalizedX, normalizedY);
        
        // 10. Trigger event for UI updates
        if (isInsideScreen || restrictToPanel)
        {
            OnGazePositionUpdated?.Invoke(lastPositionNormalized);
            
            // 11. Debug info
            if (showDebugInfo && Time.frameCount % 60 == 0) // Every 60 frames
            {
                Debug.Log($"Head gaze pos: ({lastPositionNormalized.x:F2}, {lastPositionNormalized.y:F2})");
            }
        }
    }
    
    // Public method to get the last calculated point (0-1 normalized)
    public Vector2 GetHeadPositionProjection()
    {
        return lastPositionNormalized;
    }
    
    // Public method to check if a valid point is available
    public bool HasValidProjection()
    {
        // Values outside 0-1 would indicate an invalid projection
        return lastPositionNormalized.x >= 0 && lastPositionNormalized.x <= 1 &&
               lastPositionNormalized.y >= 0 && lastPositionNormalized.y <= 1;
    }
    
    // Calculate the current Z distance between head and screen in cm
    public float GetZDistance()
    {
        if (faceProvider == null || !faceProvider.IsFaceTracked())
            return 0f;
            
        Vector3 headPosition = faceProvider.GetFacePosition();
        Vector3 screenPosition = Vector3.zero; // Using world origin as screen position
        Vector3 screenNormal = Vector3.forward; // Assuming screen faces forward in world space
        
        // Vector from head to screen
        Vector3 headToScreen = screenPosition - headPosition;
        
        // Distance from head to screen plane
        float distance = Vector3.Dot(headToScreen, screenNormal);
        
        // Convert to cm (Unity units are meters by default)
        return distance * 100f;
    }
    
    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (faceProvider != null)
        {
            faceProvider.OnFaceDetected -= OnHeadDetected;
            faceProvider.OnFaceLost -= OnHeadLost;
        }
    }
}