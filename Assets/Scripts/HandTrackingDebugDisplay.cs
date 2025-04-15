using UnityEngine;
using TMPro;
using System.Text;

/// <summary>
/// Displays hand tracking debug information on the DebugManager.
/// Shows current hand position, gestures, and 3D world position.
/// </summary>
public class HandTrackingDebugDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI handTrackingText;
    
    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.1f; // How often to update (seconds)
    [SerializeField] private bool showDebugInfo = true;
    
    [Header("Display Options")]
    [SerializeField] private bool show2DPosition = true;
    [SerializeField] private bool show3DPosition = true;
    [SerializeField] private bool showPalmSize = true;
    [SerializeField] private bool showGestures = true;
    
    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.gray;
    [SerializeField] private Color pointingColor = Color.yellow;
    
    // References to tracking components
    private HandLandmarkExtractor handLandmarkExtractor;
    private HandPositionInterpolator handPositionInterpolator;
    private HandTrackingConnector handTrackingConnector;
    
    // Update timer
    private float lastUpdateTime = 0f;
    
    private void Start()
    {
        // Find necessary components
        handLandmarkExtractor = HandLandmarkExtractor.Instance;
        handPositionInterpolator = HandPositionInterpolator.Instance;
        handTrackingConnector = FindObjectOfType<HandTrackingConnector>();
        
        // Initialize UI
        if (handTrackingText != null)
        {
            handTrackingText.text = "Waiting for hand tracking...";
            handTrackingText.color = inactiveColor;
        }
    }
    
    private void Update()
    {
        if (!showDebugInfo || handTrackingText == null)
            return;
            
        // Update at specified interval
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateHandTrackingDisplay();
            lastUpdateTime = Time.time;
        }
    }
    
    private void UpdateHandTrackingDisplay()
    {
        // Check if hand tracking is available
        bool isHandDetected = handLandmarkExtractor != null && handLandmarkExtractor.IsHandDetected;
        
        // Set text color based on tracking status
        if (isHandDetected)
        {
            bool isPointing = handLandmarkExtractor.IsPointing;
            handTrackingText.color = isPointing ? pointingColor : activeColor;
        }
        else
        {
            handTrackingText.color = inactiveColor;
            handTrackingText.text = "No hand detected";
            return;
        }
        
        // Build display text
        StringBuilder text = new StringBuilder();
        text.AppendLine("<b>Hand Tracking</b>");
        
        // 2D position (normalized screen space)
        if (show2DPosition && handLandmarkExtractor != null)
        {
            Vector3 indexTip = handLandmarkExtractor.GetIndexFingerTipPosition();
            text.AppendLine($"2D Pos: ({indexTip.x:F2}, {indexTip.y:F2})");
        }
        
        // 3D position (world space)
        if (show3DPosition && handPositionInterpolator != null && handPositionInterpolator.IsHandVisible())
        {
            Vector3 worldPos = handPositionInterpolator.GetCurrentPosition();
            float depth = handPositionInterpolator.GetEstimatedDepth();
            text.AppendLine($"3D Pos: ({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})");
            text.AppendLine($"Depth: {depth:F2}m");
        }
        
        // Palm size
        if (showPalmSize && handLandmarkExtractor != null)
        {
            float palmSize = handLandmarkExtractor.GetPalmSize();
            text.AppendLine($"Palm Size: {palmSize:F3}");
        }
        
        // Gestures
        if (showGestures && handLandmarkExtractor != null)
        {
            bool isPointing = handLandmarkExtractor.IsPointing;
            bool isPinching = handLandmarkExtractor.IsPinching;
            bool isHandOpen = handLandmarkExtractor.IsHandOpen;
            
            text.AppendLine("Gestures:");
            text.AppendLine($"- Pointing: {(isPointing ? "<color=#FFFF00>YES</color>" : "no")}");
            text.AppendLine($"- Pinching: {(isPinching ? "<color=#FFFF00>YES</color>" : "no")}");
            text.AppendLine($"- Open: {(isHandOpen ? "<color=#FFFF00>YES</color>" : "no")}");
        }
        
        // Update UI
        handTrackingText.text = text.ToString();
    }
    
    // Public methods for external control
    
    public void ToggleDebugInfo()
    {
        showDebugInfo = !showDebugInfo;
        
        if (handTrackingText != null)
        {
            handTrackingText.gameObject.SetActive(showDebugInfo);
        }
    }
    
    public void Set2DPositionVisible(bool visible)
    {
        show2DPosition = visible;
    }
    
    public void Set3DPositionVisible(bool visible)
    {
        show3DPosition = visible;
    }
    
    public void SetPalmSizeVisible(bool visible)
    {
        showPalmSize = visible;
    }
    
    public void SetGesturesVisible(bool visible)
    {
        showGestures = visible;
    }
}