using UnityEngine;

/// <summary>
/// Connects HandLandmarkExtractor with HandPositionInterpolator to convert
/// MediaPipe's 2D hand tracking data to 3D space.
/// </summary>
public class HandTrackingConnector : MonoBehaviour
{
    [SerializeField] private bool enableHandTracking = true;
    [SerializeField] private float palmSizeMultiplier = 1.0f;
    [SerializeField] private HandPositionInterpolator handPositionInterpolator;
    
    // Reference to the hand landmark extractor
    private HandLandmarkExtractor handLandmarkExtractor;
    
    // Last position data
    private Vector2 lastHandPosition = new Vector2(0.5f, 0.5f);
    private float lastPalmSize = 0.1f;
    private bool lastPointingState = false;
    
    private void Start()
    {
        // Try to find HandPositionInterpolator if not assigned
        if (handPositionInterpolator == null)
        {
            handPositionInterpolator = FindObjectOfType<HandPositionInterpolator>();
            if (handPositionInterpolator == null)
            {
                Debug.LogWarning("HandPositionInterpolator not found. Please assign it in the inspector.");
            }
        }
        
        // Subscribe to Hand Landmark events
        handLandmarkExtractor = HandLandmarkExtractor.Instance;
        
        if (handLandmarkExtractor != null)
        {
            handLandmarkExtractor.OnHandDetected += OnHandDetected;
            handLandmarkExtractor.OnHandLost += OnHandLost;
            handLandmarkExtractor.OnPointingStart += OnPointingStart;
            handLandmarkExtractor.OnPointingEnd += OnPointingEnd;
        }
        else
        {
            Debug.LogWarning("HandLandmarkExtractor not found.");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (handLandmarkExtractor != null)
        {
            handLandmarkExtractor.OnHandDetected -= OnHandDetected;
            handLandmarkExtractor.OnHandLost -= OnHandLost;
            handLandmarkExtractor.OnPointingStart -= OnPointingStart;
            handLandmarkExtractor.OnPointingEnd -= OnPointingEnd;
        }
    }
    
    private void Update()
    {
        if (!enableHandTracking || handLandmarkExtractor == null || handPositionInterpolator == null)
            return;
            
        // Only process if hand is detected
        if (handLandmarkExtractor.IsHandDetected)
        {
            // Get index finger tip as main tracking point
            Vector3 indexFingerTip = handLandmarkExtractor.GetIndexFingerTipPosition();
            lastHandPosition = new Vector2(indexFingerTip.x, indexFingerTip.y);
            
            // Calculate palm size
            lastPalmSize = handLandmarkExtractor.GetPalmSize() * palmSizeMultiplier;
            
            // Check pointing state
            lastPointingState = handLandmarkExtractor.IsPointing;
            
            // Update the interpolator
            handPositionInterpolator.UpdateHandPosition(lastHandPosition, lastPalmSize, lastPointingState);
        }
    }
    
    // Event handlers
    
    private void OnHandDetected()
    {
        // Hand was detected - nothing special to do here
    }
    
    private void OnHandLost()
    {
        // Use MainThreadDispatcher to ensure the method runs on the main thread
        NetworkFramework.MainThreadDispatcher.RunOnMainThread(() => {
            if (handPositionInterpolator != null)
            {
                handPositionInterpolator.ClearHandData();
            }
        });
    }
    
    private void OnPointingStart()
    {
        lastPointingState = true;
    }
    
    private void OnPointingEnd()
    {
        lastPointingState = false;
    }
    
    // Public methods
    
    public void ToggleHandTracking()
    {
        enableHandTracking = !enableHandTracking;
        
        if (!enableHandTracking && handPositionInterpolator != null)
        {
            handPositionInterpolator.ClearHandData();
        }
    }
    
    public Vector3 GetWorldPosition()
    {
        if (handPositionInterpolator != null)
        {
            return handPositionInterpolator.GetCurrentPosition();
        }
        return Vector3.zero;
    }
    
    public bool IsTracking()
    {
        return enableHandTracking && 
               handLandmarkExtractor != null && 
               handLandmarkExtractor.IsHandDetected;
    }
    
    public bool IsPointingActive()
    {
        return IsTracking() && lastPointingState;
    }
}