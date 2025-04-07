using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplifiedHandTracking : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private KeyCode toggleTrackingKey = KeyCode.L;
    [SerializeField] private bool useManualPosition = false;
    
    [Header("Simulation Settings")]
    [SerializeField] private float movementSpeed = 0.5f; // Reduced from 2.0
    [SerializeField] private float movementRange = 0.1f; // Reduced from 0.3
    
    [Header("Manual Position")]
    [SerializeField] private Vector3 manualHandPosition = Vector3.zero;
    
    [Header("Tracking Visualization")]
    [SerializeField] private GameObject handCube; // Reference to a cube that represents the hand
    
    // Tracking status
    public bool IsHandTracked { get; private set; } = false;
    
    // Hand position
    private Vector3 handPosition = Vector3.zero;
    
    void Start()
    {
        // Create cube if not assigned
        if (handCube == null)
        {
            handCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handCube.name = "HandCube";
            handCube.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f); // Smaller cube
            
            // Set material
            Renderer renderer = handCube.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.blue;
        }
        
        // Initially hide cube
        handCube.SetActive(false);
        
        Debug.Log("SimplifiedHandTracking initialized. Press 'L' to toggle tracking.");
    }
    
    void Update()
    {
        // Toggle tracking
        if (Input.GetKeyDown(toggleTrackingKey))
        {
            IsHandTracked = !IsHandTracked;
            handCube.SetActive(IsHandTracked);
            Debug.Log("Hand tracking " + (IsHandTracked ? "enabled" : "disabled"));
        }
        
        // Check if cube is active (either by tracking or manually)
        bool cubeActive = handCube != null && handCube.activeSelf;
        
        // Update hand position if tracking is enabled or cube is active
        if (IsHandTracked || cubeActive)
        {
            if (useManualPosition)
            {
                // Use manual position
                handPosition = manualHandPosition;
            }
            else
            {
                // Use simulated movement
                SimulateHandMovement();
            }
            
            UpdateCubePosition();
        }
        
        // Update tracking status if cube was manually activated
        if (cubeActive && !IsHandTracked)
        {
            IsHandTracked = true;
        }
    }
    
    void SimulateHandMovement()
    {
        // Create a smooth oscillating movement (reduced amplitude)
        float xOffset = Mathf.Sin(Time.time * movementSpeed) * movementRange;
        float yOffset = Mathf.Cos(Time.time * movementSpeed * 0.7f) * movementRange;
        float zOffset = Mathf.Sin(Time.time * movementSpeed * 0.5f) * movementRange;
        
        // Update hand position
        handPosition = new Vector3(xOffset, yOffset, zOffset);
    }
    
    void UpdateCubePosition()
    {
        if (handCube != null)
        {
            handCube.transform.position = handPosition;
        }
    }
    
    // Public method to set position manually
    public void SetHandPosition(Vector3 position)
    {
        manualHandPosition = position;
        // Switch to manual mode automatically
        useManualPosition = true;
    }
    
    // Public method to get the hand position for network transmission
    public Vector3 GetHandPosition()
    {
        return IsHandTracked ? handPosition : Vector3.zero;
    }
    
    // Public method to get formatted data for UDP
    public string GetHandDataForUDP()
    {
        if (!IsHandTracked) return "";
        
        return string.Format("HAND:{0:F2},{1:F2},{2:F2}", 
            handPosition.x, handPosition.y, handPosition.z);
    }
    
    // Debug UI
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, Screen.height - 160, 300, 150));
        GUILayout.Label("Simplified Hand Tracking");
        
        string trackingStatus = IsHandTracked ? "TRACKED" : "NOT TRACKED";
        GUILayout.Label("Status: " + trackingStatus);
        
        string positionModeLabel = useManualPosition ? "MANUAL" : "SIMULATED";
        GUILayout.Label("Position Mode: " + positionModeLabel);
        
        if (IsHandTracked)
        {
            GUILayout.Label("Position: " + handPosition.ToString("F2"));
        }
        
        // Toggle for manual positioning
        bool newManualMode = GUILayout.Toggle(useManualPosition, "Use Manual Position");
        if (newManualMode != useManualPosition)
        {
            useManualPosition = newManualMode;
        }
        
        GUILayout.Label("Press L to toggle tracking");
        GUILayout.EndArea();
    }
}