using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System;

[RequireComponent(typeof(ARFace))]
public class FaceTransformProvider : MonoBehaviour
{
    // Singleton pattern for easy access
    public static FaceTransformProvider Instance { get; private set; }
    
    // Events
    public event Action OnFaceDetected;
    public event Action OnFaceLost;
    
    // AR Face reference
    private ARFace arFace;
    
    // Face transform data
    private Vector3 facePosition;
    private Quaternion faceRotation;
    private Vector3 faceForward;
    private Vector3 faceUp;
    private Vector3 faceRight;
    
    // Tracking state
    private bool isFaceTracked = false;
    
    // Debug settings
    [SerializeField] private bool showDebugInfo = false;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        
        // Get ARFace component
        arFace = GetComponent<ARFace>();
    }
    
    private void OnEnable()
    {
        if (arFace != null)
        {
            arFace.updated += OnFaceUpdated;
        }
    }
    
    private void OnDisable()
    {
        if (arFace != null)
        {
            arFace.updated -= OnFaceUpdated;
        }
    }
    
    private void OnFaceUpdated(ARFaceUpdatedEventArgs args)
    {
        // Check if face is being tracked
        bool wasTracked = isFaceTracked;
        isFaceTracked = arFace.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking;
        
        // If face was just detected
        if (!wasTracked && isFaceTracked)
        {
            if (showDebugInfo)
                Debug.Log("Face detected");
                
            OnFaceDetected?.Invoke();
        }
        // If face was just lost
        else if (wasTracked && !isFaceTracked)
        {
            if (showDebugInfo)
                Debug.Log("Face lost");
                
            OnFaceLost?.Invoke();
        }
        
        // Update transform data when tracked
        if (isFaceTracked)
        {
            UpdateFaceTransformData();
        }
    }
    
    private void UpdateFaceTransformData()
    {
        // Get current face transform data
        facePosition = transform.position;
        faceRotation = transform.rotation;
        faceForward = transform.forward;
        faceUp = transform.up;
        faceRight = transform.right;
        
        // Debug logging
        if (showDebugInfo && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"Face Pos: {facePosition}, Rot: {faceRotation.eulerAngles}");
        }
    }
    
    // Public methods for accessing face transform data
    
    public bool IsFaceTracked()
    {
        return isFaceTracked;
    }
    
    public Vector3 GetFacePosition()
    {
        return facePosition;
    }
    
    public Quaternion GetFaceRotation()
    {
        return faceRotation;
    }
    
    public Vector3 GetFaceForward()
    {
        return faceForward;
    }
    
    public Vector3 GetFaceUp()
    {
        return faceUp;
    }
    
    public Vector3 GetFaceRight()
    {
        return faceRight;
    }
    
    public Transform GetFaceTransform()
    {
        return isFaceTracked ? transform : null;
    }
    
}