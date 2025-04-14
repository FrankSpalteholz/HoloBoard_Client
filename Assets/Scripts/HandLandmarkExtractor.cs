using System;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe;
using Mediapipe.Tasks.Components.Containers;

/// <summary>
/// Extracts and processes hand landmark data from MediaPipe.
/// Implements gesture recognition with smoothing and support for different hand gestures.
/// </summary>
public class HandLandmarkExtractor
{
    // Singleton Pattern
    private static HandLandmarkExtractor _instance;
    public static HandLandmarkExtractor Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new HandLandmarkExtractor();
            }
            return _instance;
        }
    }

    // Events
    public event Action OnHandDetected;
    public event Action OnHandLost;
    public event Action OnPinchStart;
    public event Action OnPinchEnd;
    public event Action OnPointingStart;
    public event Action OnPointingEnd;
    public event Action OnHandOpenStart;
    public event Action OnHandOpenEnd;

    // Hand Landmarks
    public List<Vector3> CurrentLandmarks { get; private set; } = new List<Vector3>();
    public string CurrentHandedness { get; private set; } = "";
    
    // Enum for detected hand gestures
    public enum HandGesture
    {
        Neutral,
        Pinching,
        Pointing,
        HandOpen
    }
    
    // Current gesture
    private HandGesture _currentGesture = HandGesture.Neutral;
    public HandGesture CurrentGesture => _currentGesture;
    
    // Getters for individual gestures
    public bool IsPinching => _currentGesture == HandGesture.Pinching;
    public bool IsPointing => _currentGesture == HandGesture.Pointing;
    public bool IsHandOpen => _currentGesture == HandGesture.HandOpen;
    public bool IsNeutral => _currentGesture == HandGesture.Neutral;
    
    // Detection status
    private bool _wasHandDetected = false;
    public bool IsHandDetected => CurrentLandmarks.Count > 0;
    
    // Frame counting for stability
    private int _gestureFrameCount = 0;
    private HandGesture _detectedGesture = HandGesture.Neutral;
    
    // Number of frames a gesture must be detected before activation
    private const int FRAMES_TO_ACTIVATE = 2;
    
    // Thresholds for gesture detection - dynamic based on hand size
    private const float PINCH_THRESHOLD_FACTOR = 0.2f;  // Factor of hand size for pinch
    private const float POINTING_THRESHOLD_FACTOR = 1.5f; // Factor for pointing (ratio index finger to others)
    private const float HAND_OPEN_THRESHOLD_FACTOR = 0.35f; // Factor of hand size for open hand
    
    // Smoothing parameters
    public float SmoothingFactor = 0.7f;
    private List<Vector3> _smoothedLandmarks = new List<Vector3>();
    
    // Cache for specific landmarks
    private Dictionary<int, Vector3> _cachedLandmarks = new Dictionary<int, Vector3>();
    
    // Cache for calculated distances
    private Dictionary<string, float> _distanceCache = new Dictionary<string, float>();
    
    // Hand size for dynamic thresholds
    private float _palmSize = 0.1f; // Default value

    // Private Constructor for Singleton Pattern
    private HandLandmarkExtractor() { }

    /// <summary>
    /// Updates the hand landmark data from MediaPipe landmarks
    /// </summary>
    /// <param name="list">NormalizedLandmarks from MediaPipe</param>
    public void UpdateFrom(NormalizedLandmarks list)
    {
        bool hadLandmarks = CurrentLandmarks.Count > 0;
        
        CurrentLandmarks.Clear();
        _cachedLandmarks.Clear();
        _distanceCache.Clear();
        
        if (list.landmarks == null)
        {
            // If no hand is detected anymore
            if (hadLandmarks)
            {
                // Hand was lost
                OnHandLost?.Invoke();
                _wasHandDetected = false;
                
                // Reset gesture to Neutral
                SetGesture(HandGesture.Neutral);
                _gestureFrameCount = 0;
                _detectedGesture = HandGesture.Neutral;
            }
            return;
        }
        
        // Process new landmarks
        foreach (var lm in list.landmarks)
        {
            // Mirror X-coordinate (1.0 - x) to swap left and right
            Vector3 mirroredPos = new Vector3(1.0f - lm.x, lm.y, lm.z);
            CurrentLandmarks.Add(mirroredPos);
        }
        
        // Apply smoothing if desired
        if (SmoothingFactor > 0 && _smoothedLandmarks.Count == CurrentLandmarks.Count)
        {
            for (int i = 0; i < CurrentLandmarks.Count; i++)
            {
                _smoothedLandmarks[i] = Vector3.Lerp(CurrentLandmarks[i], _smoothedLandmarks[i], SmoothingFactor);
                CurrentLandmarks[i] = _smoothedLandmarks[i];
            }
        }
        else
        {
            // If the number of landmarks doesn't match, reinitialize the smoothed list
            _smoothedLandmarks.Clear();
            _smoothedLandmarks.AddRange(CurrentLandmarks);
        }
        
        // Hand detection events and status
        if (!hadLandmarks && CurrentLandmarks.Count > 0)
        {
            // Hand was newly detected
            OnHandDetected?.Invoke();
            _wasHandDetected = true;
        }
        
        // Calculate hand size for dynamic thresholds
        _palmSize = CalculatePalmSize();
        
        // Check gestures
        UpdateGestures();
    }
    
    /// <summary>
    /// Explicitly sets that no hand is detected (for external calls)
    /// </summary>
    public void ClearHandData()
    {
        CurrentLandmarks.Clear();
        _cachedLandmarks.Clear();
        _distanceCache.Clear();
        _smoothedLandmarks.Clear();
        
        // Reset gesture to Neutral
        SetGesture(HandGesture.Neutral);
        _gestureFrameCount = 0;
        _detectedGesture = HandGesture.Neutral;
        _wasHandDetected = false;
        CurrentHandedness = "";
        _palmSize = 0;
        
        // Trigger event if hand was previously detected
        OnHandLost?.Invoke();
    }
    
    // Calculate hand size (distance between wrist and middle finger base joint)
    private float CalculatePalmSize()
    {
        return GetDistanceBetweenLandmarks(0, 9);
    }
    
    /// <summary>
    /// Public method to get hand size (for debug)
    /// </summary>
    public float GetPalmSize()
    {
        return _palmSize;
    }

    /// <summary>
    /// Sets the handedness of the detected hand
    /// </summary>
    public void SetHandedness(string handedness)
    {
        // Flip handedness (left becomes right, right becomes left)
        if (handedness.ToLower().Contains("left"))
        {
            CurrentHandedness = "Right";
        }
        else if (handedness.ToLower().Contains("right"))
        {
            CurrentHandedness = "Left";
        }
        else
        {
            // If handedness not recognized, use original value
            CurrentHandedness = handedness;
        }
    }
    
    /// <summary>
    /// Updates the current gesture based on the detected landmarks
    /// </summary>
    private void UpdateGestures()
    {
        if (CurrentLandmarks.Count < 21) return;
        
        // Calculate hand size for dynamic thresholds
        _palmSize = CalculatePalmSize();
        
        // Determine currently detected gesture
        HandGesture detectedGesture = HandGesture.Neutral; // Default is Neutral
        
        // Check each gesture - Neutral is now treated equally
        bool isPinch = DetectPinch();
        bool isPointing = DetectPointing();
        bool isHandOpen = DetectHandOpen();
        bool isNeutral = DetectNeutral();
        
        // Prioritization of gestures
        if (isPinch) {
            detectedGesture = HandGesture.Pinching;
        }
        else if (isPointing) {
            detectedGesture = HandGesture.Pointing;
        }
        else if (isHandOpen) {
            detectedGesture = HandGesture.HandOpen;
        }
        else if (isNeutral) {
            detectedGesture = HandGesture.Neutral;
        }
        
        // If we detect a new gesture, start the frame counter
        if (detectedGesture != _detectedGesture)
        {
            _detectedGesture = detectedGesture;
            _gestureFrameCount = 0;
        }
        else
        {
            // Same gesture as before - increase frame counter
            _gestureFrameCount++;
        }
        
        // If a gesture has been detected for enough frames, activate it
        if (_gestureFrameCount >= FRAMES_TO_ACTIVATE && _currentGesture != _detectedGesture)
        {
            SetGesture(_detectedGesture);
        }
    }
    
    /// <summary>
    /// Sets the current gesture and triggers appropriate events
    /// </summary>
    private void SetGesture(HandGesture newGesture)
    {
        // If gesture doesn't change, do nothing
        if (newGesture == _currentGesture)
            return;
            
        // Call End-Events for old gesture
        switch (_currentGesture)
        {
            case HandGesture.Pinching:
                OnPinchEnd?.Invoke();
                break;
            case HandGesture.Pointing:
                OnPointingEnd?.Invoke();
                break;
            case HandGesture.HandOpen:
                OnHandOpenEnd?.Invoke();
                break;
        }
        
        // Set new gesture
        _currentGesture = newGesture;
        
        // Call Start-Events for new gesture
        switch (_currentGesture)
        {
            case HandGesture.Pinching:
                OnPinchStart?.Invoke();
                break;
            case HandGesture.Pointing:
                OnPointingStart?.Invoke();
                break;
            case HandGesture.HandOpen:
                OnHandOpenStart?.Invoke();
                break;
        }
    }
    
    /// <summary>
    /// Pinch gesture detection: thumb and index finger close together
    /// </summary>
    private bool DetectPinch()
    {
        // Basic pinch test: thumb and index finger close together
        float pinchDistance = GetDistanceBetweenLandmarks(4, 8); // 4 = thumb tip, 8 = index finger tip
        float pinchThreshold = _palmSize * PINCH_THRESHOLD_FACTOR;
        
        // Only check distance between thumb and index finger
        return pinchDistance < pinchThreshold;
    }
    
    /// <summary>
    /// Pointing gesture detection: index finger extended, other fingers bent
    /// </summary>
    private bool DetectPointing()
    {
        // 1. Measure finger lengths (from joint to tip)
        float indexLength = GetDistanceBetweenLandmarks(5, 8);  // Index finger
        float middleLength = GetDistanceBetweenLandmarks(9, 12); // Middle finger
        float ringLength = GetDistanceBetweenLandmarks(13, 16);  // Ring finger
        float pinkyLength = GetDistanceBetweenLandmarks(17, 20); // Little finger
        
        // 2. Minimum length for index finger (prevents false detection with relaxed hand)
        float minIndexLength = _palmSize * 0.5f;  // Increased to 0.5f - index finger must be clearly extended
        
        // 3. Calculate average length of other fingers
        float otherFingersAvgLength = (middleLength + ringLength + pinkyLength) / 3.0f;
        
        // 4. Index finger must be significantly longer than the other fingers
        bool isPointing = indexLength > minIndexLength && 
                         indexLength > otherFingersAvgLength * 2.0f; // Increased to 2.0 - index finger must be twice as long
        
        // 5. Additional test: index finger must be significantly further from palm
        Vector3 palmCenter = GetPalmCenter();
        float indexToPalm = Vector3.Distance(GetLandmarkPosition(8), palmCenter);
        float otherFingersToPalm = (
            Vector3.Distance(GetLandmarkPosition(12), palmCenter) +
            Vector3.Distance(GetLandmarkPosition(16), palmCenter) +
            Vector3.Distance(GetLandmarkPosition(20), palmCenter)
        ) / 3.0f;
        
        bool indexExtended = indexToPalm > otherFingersToPalm * 1.8f; // Increased to 1.8f
        
        // 6. NEW: Check distance between index fingertip and other fingertips
        float indexToMiddle = GetDistanceBetweenLandmarks(8, 12);
        float indexToRing = GetDistanceBetweenLandmarks(8, 16);
        float indexToPinky = GetDistanceBetweenLandmarks(8, 20);
        
        // Calculate average distance
        float avgTipDistance = (indexToMiddle + indexToRing + indexToPinky) / 3.0f;
        
        // Distance must be at least 50% of hand size for a real pointing finger
        bool tipsSeparated = avgTipDistance > _palmSize * 0.5f;
        
        // All tests must pass for a real pointing gesture
        return isPointing && indexExtended && tipsSeparated;
    }
    
    /// <summary>
    /// Hand-Open gesture detection: All fingers extended and spread
    /// </summary>
    private bool DetectHandOpen()
    {
        // If pinch gesture is already detected, hand cannot be open
        if (DetectPinch()) return false;
        
        // Check if all fingers have a minimum length
        float thumbLength = GetDistanceBetweenLandmarks(1, 4);
        float indexLength = GetDistanceBetweenLandmarks(5, 8);
        float middleLength = GetDistanceBetweenLandmarks(9, 12);
        float ringLength = GetDistanceBetweenLandmarks(13, 16);
        float pinkyLength = GetDistanceBetweenLandmarks(17, 20);
        
        float minLength = _palmSize * HAND_OPEN_THRESHOLD_FACTOR;
        
        // All fingers must be extended
        bool allFingersExtended = thumbLength > minLength &&
                                 indexLength > minLength &&
                                 middleLength > minLength &&
                                 ringLength > minLength &&
                                 pinkyLength > minLength;
        
        // Additionally: Check the distance between fingertips (fingers must be spread)
        float spreadDistance = GetDistanceBetweenLandmarks(4, 20); // Thumb to little finger
        float minSpreadDistance = _palmSize * 0.6f;
        
        return allFingersExtended && spreadDistance > minSpreadDistance;
    }
    
    /// <summary>
    /// Determines if the hand is in Neutral position (relaxed posture)
    /// </summary>
    private bool DetectNeutral()
    {
        // In neutral position, fingertips are in a relaxed position 
        // near the palm, but not specifically in any of the other gestures
        
        // 1. Calculate distance of all fingertips to palm
        Vector3 palmCenter = GetPalmCenter();
        float thumbDist = Vector3.Distance(GetLandmarkPosition(4), palmCenter);
        float indexDist = Vector3.Distance(GetLandmarkPosition(8), palmCenter);
        float middleDist = Vector3.Distance(GetLandmarkPosition(12), palmCenter);
        float ringDist = Vector3.Distance(GetLandmarkPosition(16), palmCenter);
        float pinkyDist = Vector3.Distance(GetLandmarkPosition(20), palmCenter);
        
        // 2. Calculate average distance of fingers (without thumb)
        float avgFingerDist = (indexDist + middleDist + ringDist + pinkyDist) / 4.0f;
        
        // 3. Neutral position: Fingertips are in a medium distance range to palm
        //    Not too close (closed hand) and not too far (open hand)
        float minNeutralDist = _palmSize * 0.15f;
        float maxNeutralDist = _palmSize * 0.45f;  // Increased for better neutral detection
        
        // 4. Check if all fingers have similar distances (no finger significantly longer)
        //    This prevents overlap with Pointing
        float maxDist = Mathf.Max(indexDist, middleDist, ringDist, pinkyDist);
        float minDist = Mathf.Min(indexDist, middleDist, ringDist, pinkyDist);
        float fingerDistVariation = maxDist / minDist;
        
        // Variation between longest and shortest finger should not be too large
        bool fingersUniform = fingerDistVariation < 1.5f;
        
        // 5. Check if distances are in neutral range
        bool fingersInNeutralRange = avgFingerDist >= minNeutralDist && avgFingerDist <= maxNeutralDist;
        
        // 6. For Neutral must also be ensured that it is not a Pinch gesture
        float thumbIndexDist = GetDistanceBetweenLandmarks(4, 8);
        bool notPinching = thumbIndexDist > _palmSize * 0.2f;
        
        // Hand is in neutral position if all conditions are met
        return fingersUniform && fingersInNeutralRange && notPinching;
    }

    /// <summary>
    /// Helper methods for commonly needed landmarks
    /// </summary>
    public Vector3 GetWristPosition()
    {
        return GetLandmarkPosition(0);
    }

    public Vector3 GetThumbTipPosition()
    {
        return GetLandmarkPosition(4);
    }

    public Vector3 GetIndexFingerTipPosition()
    {
        return GetLandmarkPosition(8);
    }

    public Vector3 GetMiddleFingerTipPosition()
    {
        return GetLandmarkPosition(12);
    }

    public Vector3 GetRingFingerTipPosition()
    {
        return GetLandmarkPosition(16);
    }

    public Vector3 GetPinkyTipPosition()
    {
        return GetLandmarkPosition(20);
    }
    
    /// <summary>
    /// Palm center (rough approximation)
    /// </summary>
    public Vector3 GetPalmCenter()
    {
        if (CurrentLandmarks.Count < 21) return Vector3.zero;
        
        // Midpoint between landmarks 0 (wrist) and 9 (middle of hand)
        return Vector3.Lerp(GetLandmarkPosition(0), GetLandmarkPosition(9), 0.5f);
    }

    /// <summary>
    /// Helper method to get a specific landmark by index
    /// </summary>
    public Vector3 GetLandmarkPosition(int index)
    {
        // Check if we already have the value in cache
        if (_cachedLandmarks.TryGetValue(index, out Vector3 cachedPos))
        {
            return cachedPos;
        }
        
        // If not, get it from the list and store it in cache
        if (CurrentLandmarks != null && index >= 0 && index < CurrentLandmarks.Count)
        {
            _cachedLandmarks[index] = CurrentLandmarks[index];
            return CurrentLandmarks[index];
        }
        
        return Vector3.zero;
    }

    /// <summary>
    /// Calculate distance between two landmarks with caching
    /// </summary>
    public float GetDistanceBetweenLandmarks(int index1, int index2)
    {
        string cacheKey = $"{index1}-{index2}";
        
        // Check if the distance has already been calculated
        if (_distanceCache.TryGetValue(cacheKey, out float cachedDistance))
        {
            return cachedDistance;
        }
        
        if (CurrentLandmarks == null || 
            index1 < 0 || index1 >= CurrentLandmarks.Count || 
            index2 < 0 || index2 >= CurrentLandmarks.Count)
        {
            return 0f;
        }
        
        float distance = Vector3.Distance(GetLandmarkPosition(index1), GetLandmarkPosition(index2));
        
        // Cache the result
        _distanceCache[cacheKey] = distance;
        
        return distance;
    }
    
    /// <summary>
    /// Returns the palm normal (useful for direction detection)
    /// </summary>
    public Vector3 GetPalmNormal()
    {
        if (CurrentLandmarks.Count < 21) return Vector3.forward;
        
        // Calculate two vectors on the palm
        Vector3 v1 = GetLandmarkPosition(5) - GetLandmarkPosition(17); // From index finger to little finger
        Vector3 v2 = GetLandmarkPosition(9) - GetLandmarkPosition(0);  // From hand center to wrist
        
        // Calculate normal with cross product
        return Vector3.Cross(v1, v2).normalized;
    }
}