using UnityEngine;

public class TrackingMirrorCorrector : MonoBehaviour
{
    [Header("Source Tracking")]
    [Tooltip("The transform that receives the raw tracking data")]
    public Transform sourceTransform;
    
    [Header("Mirror Settings")]
    [Tooltip("Invert the X-axis position")]
    public bool invertPositionX = true;
    
    [Tooltip("Invert the Y-axis position")]
    public bool invertPositionY = false;
    
    [Tooltip("Invert the Z-axis position")]
    public bool invertPositionZ = false;
    
    [Tooltip("Invert the X-axis rotation")]
    public bool invertRotationX = false;
    
    [Tooltip("Invert the Y-axis rotation")]
    public bool invertRotationY = true;
    
    [Tooltip("Invert the Z-axis rotation")]
    public bool invertRotationZ = true;
    
    [Header("Position Offset")]
    [Tooltip("Additional position offset to apply after mirroring")]
    public Vector3 positionOffset = Vector3.zero;
    
    [Header("Rotation Offset")]
    [Tooltip("Additional rotation offset to apply after mirroring (in degrees)")]
    public Vector3 rotationOffset = Vector3.zero;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    private Vector3 sourcePosition;
    private Quaternion sourceRotation;
    private Vector3 correctedPosition;
    private Quaternion correctedRotation;
    
    void Start()
    {
        if (sourceTransform == null)
        {
            Debug.LogError("Source transform not assigned! Please assign a transform to mirror.");
            enabled = false;
        }
    }
    
    void LateUpdate()
    {
        if (sourceTransform == null)
            return;
            
        // Get the current source transform values
        sourcePosition = sourceTransform.position;
        sourceRotation = sourceTransform.rotation;
        
        // Create a corrected position with selected axes inverted
        correctedPosition = new Vector3(
            invertPositionX ? -sourcePosition.x : sourcePosition.x,
            invertPositionY ? -sourcePosition.y : sourcePosition.y,
            invertPositionZ ? -sourcePosition.z : sourcePosition.z
        );
        
        // Apply position offset
        correctedPosition += positionOffset;
        
        // Get euler angles
        Vector3 sourceEuler = sourceRotation.eulerAngles;
        
        // Create corrected euler angles with selected axes inverted
        Vector3 correctedEuler = new Vector3(
            invertRotationX ? -sourceEuler.x : sourceEuler.x,
            invertRotationY ? -sourceEuler.y : sourceEuler.y,
            invertRotationZ ? -sourceEuler.z : sourceEuler.z
        );
        
        // Apply rotation offset
        correctedEuler += rotationOffset;
        
        // Convert back to quaternion
        correctedRotation = Quaternion.Euler(correctedEuler);
        
        // Apply the corrected values to this transform
        transform.position = correctedPosition;
        transform.rotation = correctedRotation;
        
        // Display debug info if enabled
        if (showDebugInfo)
        {
            Debug.Log($"Source Position: {sourcePosition}, Corrected Position: {correctedPosition}");
            Debug.Log($"Source Rotation: {sourceEuler}, Corrected Rotation: {correctedEuler}");
        }
    }
}