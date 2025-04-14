using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Central debug display for AR tracking data including facial blendshapes and hand landmarks.
/// </summary>
public class ARDebugDisplay : MonoBehaviour
{
    // Singleton implementation
    public static ARDebugDisplay Instance { get; private set; }

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showFacialBlendshapes = true;
    [SerializeField] private bool showHandLandmarks = true;
    [SerializeField] private bool showHandGestures = true;
    [SerializeField] private bool showPerformanceStats = true;
    [SerializeField] private float updateInterval = 0.1f; // How often text is updated (seconds)

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private TextMeshProUGUI faceBlendshapesText;
    [SerializeField] private TextMeshProUGUI handLandmarksText;
    [SerializeField] private TextMeshProUGUI performanceText;

    // Performance tracking
    private float deltaTime = 0.0f;
    private int frameCount = 0;
    private float fps = 0.0f;
    private float fpsUpdateTime = 0.0f;

    private void Awake()
    {
        // Singleton Pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
        
        // Enable/Disable debug display based on setting
        SetDebugVisibility(showDebugInfo);
    }

    private void Start()
    {
        // Start the regular update of debug information
        StartCoroutine(UpdateDebugInfoRoutine());
    }

    private void Update()
    {
        // FPS calculation
        deltaTime += Time.unscaledDeltaTime;
        frameCount++;
        
        if (deltaTime > 0.5f)
        {
            fps = frameCount / deltaTime;
            frameCount = 0;
            deltaTime = 0.0f;
        }
    }

    private IEnumerator UpdateDebugInfoRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(updateInterval);
        
        while (true)
        {
            if (showDebugInfo)
            {
                if (showFacialBlendshapes && faceBlendshapesText != null)
                {
                    UpdateFacialBlendshapesDebugInfo();
                }
                
                if (showHandLandmarks && handLandmarksText != null)
                {
                    UpdateHandLandmarksDebugInfo();
                }
                
                if (showPerformanceStats && performanceText != null)
                {
                    UpdatePerformanceDebugInfo();
                }
            }
            
            yield return wait;
        }
    }

    private void UpdateFacialBlendshapesDebugInfo()
    {
        if (ARFaceBlendshapeExtractor.Instance != null)
        {
            FacialBlendshapes blendshapes = ARFaceBlendshapeExtractor.Instance.GetFacialBlendshapes();
            faceBlendshapesText.text = $"<b>Face Blendshapes:</b>\n{blendshapes.ToString()}";
        }
        else
        {
            faceBlendshapesText.text = "<b>Face Blendshapes:</b>\nNot available";
        }
    }

    private void UpdateHandLandmarksDebugInfo()
    {
        if (HandLandmarkExtractor.Instance == null)
        {
            handLandmarksText.text = "<b>Hand Landmarks:</b>\nNot available";
            return;
        }
        
        // Default values when no hand is detected
        string handedness = "None";
        HandLandmarkExtractor.HandGesture currentGesture = HandLandmarkExtractor.HandGesture.Neutral;
        
        Vector3 wristPos = Vector3.zero;
        Vector3 indexTip = Vector3.zero;
        Vector3 thumbTip = Vector3.zero;
        
        // Debug values for gestures
        float pinchDistance = 0;
        float pinchThreshold = 0;
        float palmSize = 0;
        
        // If a hand is detected, override these values
        if (HandLandmarkExtractor.Instance.IsHandDetected)
        {
            handedness = HandLandmarkExtractor.Instance.CurrentHandedness;
            currentGesture = HandLandmarkExtractor.Instance.CurrentGesture;
            
            wristPos = HandLandmarkExtractor.Instance.GetWristPosition();
            indexTip = HandLandmarkExtractor.Instance.GetIndexFingerTipPosition();
            thumbTip = HandLandmarkExtractor.Instance.GetThumbTipPosition();
            
            // Calculate debug values
            pinchDistance = HandLandmarkExtractor.Instance.GetDistanceBetweenLandmarks(4, 8);
            palmSize = HandLandmarkExtractor.Instance.GetPalmSize();
            pinchThreshold = palmSize * 0.2f;
        }
        else
        {
            // If no hand is detected, explicitly show "None"
            handedness = "None";
            currentGesture = HandLandmarkExtractor.HandGesture.Neutral;
        }
        
        string handInfo = $"<b>Hand Landmarks:</b>\n" +
                         $"Hand: {handedness}\n" +
                         $"Wrist: ({wristPos.x:F2}, {wristPos.y:F2})\n" +
                         $"Index: ({indexTip.x:F2}, {indexTip.y:F2})\n" +
                         $"Thumb: ({thumbTip.x:F2}, {thumbTip.y:F2})\n";
        
        if (showHandGestures)
        {
            bool isPinching = currentGesture == HandLandmarkExtractor.HandGesture.Pinching;
            bool isPointing = currentGesture == HandLandmarkExtractor.HandGesture.Pointing;
            bool isHandOpen = currentGesture == HandLandmarkExtractor.HandGesture.HandOpen;
            bool isNeutral = currentGesture == HandLandmarkExtractor.HandGesture.Neutral;
            
            handInfo += $"\n<b>Gestures:</b>\n" +
                       $"Current: {currentGesture}\n" +
                       $"Pinching: {(isPinching ? "1" : "0")}\n" +
                       $"Pointing: {(isPointing ? "1" : "0")}\n" +
                       $"Hand Open: {(isHandOpen ? "1" : "0")}\n" +
                       $"Neutral: {(isNeutral ? "1" : "0")}\n";
                       
        }
        
        handLandmarksText.text = handInfo;
    }

    private void UpdatePerformanceDebugInfo()
    {
        // Calculate memory usage
        float totalMemory = (float)System.GC.GetTotalMemory(false) / (1024 * 1024); // In MB
        
        performanceText.text = $"<b>Performance:</b>\n" +
                              $"FPS: {fps:F1}\n" +
                              $"Memory: {totalMemory:F1} MB";
    }

    // Public methods to turn debug display on/off
    public void SetDebugVisibility(bool visible)
    {
        showDebugInfo = visible;
        
        // Enable/Disable all debug texts
        if (debugText != null) debugText.gameObject.SetActive(visible);
        if (faceBlendshapesText != null) faceBlendshapesText.gameObject.SetActive(visible && showFacialBlendshapes);
        if (handLandmarksText != null) handLandmarksText.gameObject.SetActive(visible && showHandLandmarks);
        if (performanceText != null) performanceText.gameObject.SetActive(visible && showPerformanceStats);
    }

    public void ToggleDebugVisibility()
    {
        SetDebugVisibility(!showDebugInfo);
    }

    // Additional helper methods
    public void ToggleFacialBlendshapes()
    {
        showFacialBlendshapes = !showFacialBlendshapes;
        if (faceBlendshapesText != null) faceBlendshapesText.gameObject.SetActive(showDebugInfo && showFacialBlendshapes);
    }

    public void ToggleHandLandmarks()
    {
        showHandLandmarks = !showHandLandmarks;
        if (handLandmarksText != null) handLandmarksText.gameObject.SetActive(showDebugInfo && showHandLandmarks);
    }

    public void TogglePerformanceStats()
    {
        showPerformanceStats = !showPerformanceStats;
        if (performanceText != null) performanceText.gameObject.SetActive(showDebugInfo && showPerformanceStats);
    }

    public void SetCustomDebugText(string text)
    {
        if (debugText != null)
        {
            debugText.text = text;
        }
    }
}