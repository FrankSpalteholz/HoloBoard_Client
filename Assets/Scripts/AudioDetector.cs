using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System;

public class AudioDetector : MonoBehaviour
{
    // Singleton implementation
    public static AudioDetector Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private bool detectAudio = true;
    [SerializeField] private float detectionThreshold = 0.1f; // Threshold for detection
    [SerializeField] private int sampleDataLength = 1024; // Number of audio samples per frame
    [SerializeField] private float triggerCooldown = 0.5f; // Cooldown between triggers in seconds
    [SerializeField] private float triggerDuration = 0.2f; // How long a trigger remains active
    
    [Header("Gaze Marker Visual Effects")]
    [SerializeField] private bool changeGazeMarkerColor = true;
    [SerializeField] private bool changeGazeMarkerScale = true;
    [SerializeField] private RectTransform gazeMarkerRect; // Reference to the gaze marker's RectTransform
    [SerializeField] private Image gazeMarkerImage; // Reference to the gaze marker image
    [SerializeField] private Color normalColor = Color.white; // Normal color of the gaze marker
    [SerializeField] private Color triggeredColor = Color.red; // Color to use when audio is triggered
    [SerializeField] private float scaleMultiplier = 1.2f; // Scale multiplier (20% larger)
    [SerializeField] private float effectTransitionSpeed = 5f; // Speed of visual transitions
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private TextMeshProUGUI debugText;
    
    // Private variables
    private AudioClip microphoneClip;
    private bool isMicrophoneInitialized = false;
    private float[] sampleData;
    private float currentVolume = 0f;
    private bool audioTriggered = false;
    private float lastTriggerTime = 0f;
    private string selectedDevice;
    
    // Visual effect tracking
    private Color currentMarkerColor;
    private Vector2 originalMarkerSize;
    private Vector2 currentMarkerSize;
    private Vector2 targetMarkerSize;

    // Events
    public event Action OnAudioTriggered;
    public event Action OnAudioTriggerEnded;

    void Awake()
    {
        // Singleton Pattern implementation
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
    }

    void Start()
    {
        sampleData = new float[sampleDataLength];
        
        // Store the initial gaze marker properties
        if (gazeMarkerImage != null)
        {
            currentMarkerColor = gazeMarkerImage.color;
        }
        
        if (gazeMarkerRect != null)
        {
            originalMarkerSize = gazeMarkerRect.sizeDelta;
            currentMarkerSize = originalMarkerSize;
            targetMarkerSize = originalMarkerSize;
        }
        
        // List available microphones
        if (showDebugInfo)
        {
            string micInfo = "Available Microphones:\n";
            foreach (string device in Microphone.devices)
            {
                micInfo += "- " + device + "\n";
            }
            Debug.Log(micInfo);
            
            if (debugText != null)
            {
                debugText.text = micInfo;
            }
        }
        
        // Start microphone initialization
        InitializeMicrophone();
    }

    private void InitializeMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone found!");
            isMicrophoneInitialized = false;
            return;
        }
        
        // Use the first available microphone
        selectedDevice = Microphone.devices[0];
        
        // Start recording (parameters: device, loop, length in seconds, samplerate)
        microphoneClip = Microphone.Start(selectedDevice, true, 1, 44100);
        isMicrophoneInitialized = true;
        
        // Wait a moment for microphone to initialize
        StartCoroutine(WaitForMicrophoneInitialization());
    }
    
    private IEnumerator WaitForMicrophoneInitialization()
    {
        yield return new WaitForSeconds(0.1f);
        
        // Update debug info
        if (showDebugInfo && debugText != null)
        {
            debugText.text = "Microphone initialized: " + selectedDevice;
        }
    }

    void Update()
    {
        if (!detectAudio || !isMicrophoneInitialized)
            return;
        
        // Check if an audio clip is available
        if (microphoneClip != null)
        {
            // Get current data from microphone
            int position = Microphone.GetPosition(selectedDevice);
            if (position >= 0 && microphoneClip.samples > 0)
            {
                // Calculate a start position to get the latest samples
                int startPosition = (position - sampleDataLength) % microphoneClip.samples;
                if (startPosition < 0) startPosition += microphoneClip.samples;
                
                // Get sample data
                microphoneClip.GetData(sampleData, startPosition);
                
                // Calculate average volume
                float sum = 0f;
                for (int i = 0; i < sampleDataLength; i++)
                {
                    sum += Mathf.Abs(sampleData[i]);
                }
                currentVolume = sum / sampleDataLength;
                
                // Check volume threshold
                CheckVolumeThreshold();
                
                // Update gaze marker visual effects
                UpdateGazeMarkerVisuals();
                
                // Display debug information
                if (showDebugInfo && debugText != null)
                {
                    debugText.text = string.Format("Volume: {0:F4}\nThreshold: {1:F2}\nTrigger: {2}", 
                        currentVolume, detectionThreshold, audioTriggered ? "YES" : "No");
                }
            }
        }
    }
    
    private void CheckVolumeThreshold()
    {
        // Check if cooldown has elapsed
        float timeSinceLastTrigger = Time.time - lastTriggerTime;
        
        if (currentVolume > detectionThreshold && timeSinceLastTrigger > triggerCooldown)
        {
            bool wasPreviouslyTriggered = audioTriggered;

            // Activate trigger
            audioTriggered = true;
            lastTriggerTime = Time.time;
            
            // Start timer to reset trigger after specified duration
            StartCoroutine(ResetTriggerAfterDuration());
            
            // Debug output
            Debug.Log("Audio Trigger activated! Volume: " + currentVolume);

            // Fire event if this is a new trigger
            if (!wasPreviouslyTriggered)
            {
                OnAudioTriggered?.Invoke();
            }
        }
    }
    
    private IEnumerator ResetTriggerAfterDuration()
    {
        yield return new WaitForSeconds(triggerDuration);
        bool wasPreviouslyTriggered = audioTriggered;
        audioTriggered = false;

        // Fire event only if state changed
        if (wasPreviouslyTriggered)
        {
            OnAudioTriggerEnded?.Invoke();
        }
    }
    
    private void UpdateGazeMarkerVisuals()
    {
        // Update color
        if (changeGazeMarkerColor && gazeMarkerImage != null)
        {
            // Set target color based on trigger state
            Color targetColor = audioTriggered ? triggeredColor : normalColor;
            
            // Smoothly transition to target color
            currentMarkerColor = Color.Lerp(currentMarkerColor, targetColor, Time.deltaTime * effectTransitionSpeed);
            gazeMarkerImage.color = currentMarkerColor;
        }
        
        // Update scale
        if (changeGazeMarkerScale && gazeMarkerRect != null)
        {
            // Set target size based on trigger state
            targetMarkerSize = audioTriggered ? 
                originalMarkerSize * scaleMultiplier : 
                originalMarkerSize;
            
            // Smoothly transition to target size
            currentMarkerSize = Vector2.Lerp(currentMarkerSize, targetMarkerSize, Time.deltaTime * effectTransitionSpeed);
            gazeMarkerRect.sizeDelta = currentMarkerSize;
        }
    }
    
    // Public method to query current trigger status
    public bool IsAudioTriggered()
    {
        return audioTriggered;
    }
    
    // Public method to query current volume
    public float GetCurrentVolume()
    {
        return currentVolume;
    }

    // Public method to set the detection threshold programmatically
    public void SetDetectionThreshold(float threshold)
    {
        detectionThreshold = Mathf.Max(0.01f, threshold);
    }
    
    // Ensure microphone is stopped when script is disabled
    private void OnDisable()
    {
        if (isMicrophoneInitialized)
        {
            Microphone.End(selectedDevice);
        }
        
        // Reset marker to original state
        if (gazeMarkerRect != null)
        {
            gazeMarkerRect.sizeDelta = originalMarkerSize;
        }
        
        if (gazeMarkerImage != null)
        {
            gazeMarkerImage.color = normalColor;
        }
    }

    // Method to manually trigger audio for testing
    public void TestTrigger()
    {
        bool wasPreviouslyTriggered = audioTriggered;
        audioTriggered = true;
        lastTriggerTime = Time.time;
        StartCoroutine(ResetTriggerAfterDuration());
        Debug.Log("Test Audio Trigger activated manually");

        // Fire event only if state changed
        if (!wasPreviouslyTriggered)
        {
            OnAudioTriggered?.Invoke();
        }
    }
}