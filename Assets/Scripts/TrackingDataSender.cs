using UnityEngine;
using System.Text;
using NetworkFramework;

public class CompactTrackingDataSender : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float sendRate = 0.05f; // 20 times per second
    [SerializeField] private bool autoStart = true;
    
    [Header("Component References")]
    [SerializeField] private UDPClient udpClient;
    
    // Private fields
    private float lastSendTime = 0f;
    private bool isSending = false;
    private StringBuilder jsonBuilder = new StringBuilder(256); // Pre-allocate capacity
    
    // References to data sources
    private FaceTransformProvider faceProvider;
    private ARFaceBlendshapeExtractor blendshapeExtractor;
    private HandPositionInterpolator handInterpolator;
    private HandLandmarkExtractor handLandmarkExtractor;
    private AudioDetector audioDetector;
    private GazeTrackerManager gazeTracker; // Hinzugefügt für Gazemarker-Daten
    
    private void Start()
    {
        // Find references if not assigned
        if (udpClient == null)
            udpClient = FindObjectOfType<UDPClient>();
        
        // Find all data sources
        FindDataSources();
        
        // Auto-start if configured
        if (autoStart)
            StartSending();
    }
    
    private void FindDataSources()
    {
        faceProvider = FindObjectOfType<FaceTransformProvider>();
        blendshapeExtractor = FindObjectOfType<ARFaceBlendshapeExtractor>();
        handInterpolator = FindObjectOfType<HandPositionInterpolator>();
        handLandmarkExtractor = HandLandmarkExtractor.Instance;
        audioDetector = FindObjectOfType<AudioDetector>();
        gazeTracker = GazeTrackerManager.Instance; // Referenz zum GazeTrackerManager
    }
    
    private void Update()
    {
        if (!isSending || udpClient == null)
            return;
            
        // Send at specified rate
        if (Time.time - lastSendTime > sendRate)
        {
            SendTrackingData();
            lastSendTime = Time.time;
        }
    }
    
    private void SendTrackingData()
    {
        // Reset string builder
        jsonBuilder.Clear();
        jsonBuilder.Append("{");
        
        // Add timestamp
        jsonBuilder.Append("\"t\":").Append(Time.time.ToString("F3"));
        
        // Add head transform if available
        if (faceProvider != null && faceProvider.IsFaceTracked())
        {
            Vector3 headPos = faceProvider.GetFacePosition();
            Quaternion headRot = faceProvider.GetFaceRotation();
            
            jsonBuilder.Append(",\"h\":{");
            jsonBuilder.Append("\"p\":[").Append(headPos.x.ToString("F3")).Append(",")
                       .Append(headPos.y.ToString("F3")).Append(",")
                       .Append(headPos.z.ToString("F3")).Append("],");
            
            jsonBuilder.Append("\"r\":[").Append(headRot.x.ToString("F3")).Append(",")
                       .Append(headRot.y.ToString("F3")).Append(",")
                       .Append(headRot.z.ToString("F3")).Append(",")
                       .Append(headRot.w.ToString("F3")).Append("]");
            jsonBuilder.Append("}");
            
            // Add facial blendshapes if available
            if (blendshapeExtractor != null)
            {
                FacialBlendshapes bs = blendshapeExtractor.GetFacialBlendshapes();
                
                jsonBuilder.Append(",\"bs\":{");
                jsonBuilder.Append("\"jo\":").Append(bs.JawOpen.ToString("F2")).Append(",");
                jsonBuilder.Append("\"sl\":").Append(bs.MouthSmileLeft.ToString("F2")).Append(",");
                jsonBuilder.Append("\"sr\":").Append(bs.MouthSmileRight.ToString("F2")).Append(",");
                jsonBuilder.Append("\"bl\":").Append(bs.EyeBlinkLeft.ToString("F2")).Append(",");
                jsonBuilder.Append("\"br\":").Append(bs.EyeBlinkRight.ToString("F2"));
                
                // Add other blendshapes if needed
                // jsonBuilder.Append(",\"to\":").Append(bs.TongueOut.ToString("F2"));
                
                jsonBuilder.Append("}");
            }
        }
        
        // Add hand tracking data if available
        if (handInterpolator != null && handInterpolator.IsHandVisible())
        {
            Vector3 handPos = handInterpolator.GetCurrentPosition();
            bool isPointing = handInterpolator.IsPointing();
            
            // Get handedness and gestures if available
            string handedness = "unknown";
            bool isPinching = false;
            bool isHandOpen = false;
            bool isNeutral = false;
            
            if (handLandmarkExtractor != null && handLandmarkExtractor.IsHandDetected)
            {
                handedness = handLandmarkExtractor.CurrentHandedness;
                isPinching = handLandmarkExtractor.IsPinching;
                isHandOpen = handLandmarkExtractor.IsHandOpen;
                isNeutral = handLandmarkExtractor.IsNeutral;
            }
            
            jsonBuilder.Append(",\"hand\":{");
            jsonBuilder.Append("\"p\":[").Append(handPos.x.ToString("F3")).Append(",")
                    .Append(handPos.y.ToString("F3")).Append(",")
                    .Append(handPos.z.ToString("F3")).Append("],");
            
            jsonBuilder.Append("\"side\":\"").Append(handedness).Append("\",");
            jsonBuilder.Append("\"point\":").Append(isPointing ? "true" : "false").Append(",");
            jsonBuilder.Append("\"pinch\":").Append(isPinching ? "true" : "false").Append(",");
            jsonBuilder.Append("\"open\":").Append(isHandOpen ? "true" : "false").Append(",");
            jsonBuilder.Append("\"neutral\":").Append(isNeutral ? "true" : "false");
            jsonBuilder.Append("}");
        }

        // Add audio trigger data if available
        if (audioDetector != null)
        {
            bool triggered = audioDetector.IsAudioTriggered();
            
            jsonBuilder.Append(",\"audio\":{");
            jsonBuilder.Append("\"trig\":").Append(triggered ? "true" : "false");
            jsonBuilder.Append("}");
        }

        // Add gaze marker data if available
        if (gazeTracker != null && gazeTracker.HasValidProjection())
        {
            Vector2 gazePos = gazeTracker.GetHeadPositionProjection();
            
            jsonBuilder.Append(",\"gaze\":{");
            jsonBuilder.Append("\"x\":").Append(gazePos.x.ToString("F3")).Append(",");
            jsonBuilder.Append("\"y\":").Append(gazePos.y.ToString("F3"));
            jsonBuilder.Append("}");
        }
        
        // Close JSON object
        jsonBuilder.Append("}");
        
        // Send the data
        udpClient.SendNetworkMessage(jsonBuilder.ToString());
    }
    
    // Public methods
    public void StartSending()
    {
        isSending = true;
    }
    
    public void StopSending()
    {
        isSending = false;
    }
    
    public void ToggleSending()
    {
        isSending = !isSending;
    }
    
    public void SetSendRate(float rate)
    {
        sendRate = Mathf.Max(0.01f, rate);
    }
}