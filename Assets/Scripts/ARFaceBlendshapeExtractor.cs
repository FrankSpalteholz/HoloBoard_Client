using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

#if UNITY_IOS && !UNITY_EDITOR
using Unity.Collections;
using UnityEngine.XR.ARKit;
#endif

[RequireComponent(typeof(ARFace))]
public class ARFaceBlendshapeExtractor : MonoBehaviour
{
    public static ARFaceBlendshapeExtractor Instance { get; private set; }

    [Header("Debug-Ausgabe")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private string canvasTag = "MediaPipeDebugCanvas";
    [SerializeField] private string textObjectName = "ARFacialblendshapesText";

    private TextMeshProUGUI debugText;
    private ARFace m_Face;

#if UNITY_IOS && !UNITY_EDITOR
    private ARKitFaceSubsystem m_ARKitFaceSubsystem;
#endif

    private FacialBlendshapes currentBlendshapes = new FacialBlendshapes();
    private bool blendshapesAvailable = false;

    public FacialBlendshapes GetFacialBlendshapes()
    {
        return currentBlendshapes;
    }
    
    public bool AreBlendshapesAvailable()
    {
        return blendshapesAvailable;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        m_Face = GetComponent<ARFace>();

#if UNITY_IOS && !UNITY_EDITOR
        var faceManager = FindAnyObjectByType<ARFaceManager>();
        if (faceManager != null)
        {
            m_ARKitFaceSubsystem = (ARKitFaceSubsystem)faceManager.subsystem;
        }
        blendshapesAvailable = m_ARKitFaceSubsystem != null;
#else
        // Im Editor oder auf Android sind keine Blendshapes verfügbar
        blendshapesAvailable = false;
#endif
    }

    void OnEnable()
    {
        m_Face.updated += OnUpdated;
        if (showDebugUI && debugText == null)
        {
            TryFindDebugText();
        }
    }

    void OnDisable()
    {
        m_Face.updated -= OnUpdated;
    }

    void OnUpdated(ARFaceUpdatedEventArgs eventArgs)
    {
        UpdateBlendshapeValues();
    }

    void TryFindDebugText()
    {
        if (string.IsNullOrEmpty(canvasTag) || string.IsNullOrEmpty(textObjectName)) return;
        var canvasGO = GameObject.FindGameObjectWithTag(canvasTag);
        if (canvasGO != null)
        {
            var textGO = canvasGO.transform.Find(textObjectName);
            if (textGO != null)
            {
                debugText = textGO.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    void UpdateBlendshapeValues()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (m_ARKitFaceSubsystem == null) return;

        using (var blendShapes = m_ARKitFaceSubsystem.GetBlendShapeCoefficients(m_Face.trackableId, Allocator.Temp))
        {
            blendshapesAvailable = true;
            currentBlendshapes = new FacialBlendshapes();

            float sneerLeft = 0f, sneerRight = 0f;
            float browDownLeft = 0f, browDownRight = 0f;

            foreach (var coeff in blendShapes)
            {
                switch (coeff.blendShapeLocation)
                {
                    case ARKitBlendShapeLocation.JawOpen: currentBlendshapes.JawOpen = coeff.coefficient; break;
                    case ARKitBlendShapeLocation.MouthSmileRight: currentBlendshapes.MouthSmileLeft = coeff.coefficient; break; // gespiegelt
                    case ARKitBlendShapeLocation.MouthSmileLeft: currentBlendshapes.MouthSmileRight = coeff.coefficient; break; // gespiegelt
                    case ARKitBlendShapeLocation.TongueOut: currentBlendshapes.TongueOut = coeff.coefficient; break;
                    case ARKitBlendShapeLocation.EyeBlinkRight: currentBlendshapes.EyeBlinkLeft = coeff.coefficient; break; // gespiegelt
                    case ARKitBlendShapeLocation.EyeBlinkLeft: currentBlendshapes.EyeBlinkRight = coeff.coefficient; break; // gespiegelt
                    case ARKitBlendShapeLocation.BrowDownRight: browDownLeft = coeff.coefficient; break; // gespiegelt
                    case ARKitBlendShapeLocation.BrowDownLeft: browDownRight = coeff.coefficient; break; // gespiegelt
                    case ARKitBlendShapeLocation.BrowInnerUp: currentBlendshapes.BrowInnerUp = coeff.coefficient; break;
                    case ARKitBlendShapeLocation.CheekPuff: currentBlendshapes.CheekPuff = coeff.coefficient; break;
                    case ARKitBlendShapeLocation.NoseSneerRight: sneerLeft = coeff.coefficient; break; // gespiegelt
                    case ARKitBlendShapeLocation.NoseSneerLeft: sneerRight = coeff.coefficient; break; // gespiegelt
                }
            }

            currentBlendshapes.NoseSneer = (sneerLeft + sneerRight) * 0.5f;
            currentBlendshapes.BrowDown = (browDownLeft + browDownRight) * 0.5f;
        }
#else
        // Im Editor oder auf Android: Leere Blendshapes mit 0 Werten
        blendshapesAvailable = false;
        // Die currentBlendshapes bleiben leer mit allen Werten auf 0
#endif

        // Unabhängig von der Plattform aktualisieren wir den Debug-Text
        if (showDebugUI && debugText != null)
        {
            debugText.text = currentBlendshapes.ToString();
        }
    }
    
    // Diese Methode wird vom AR Debug Display aufgerufen
    public string GetBlendshapesDebugString()
    {
        return blendshapesAvailable 
            ? currentBlendshapes.ToString() 
            : "Facial Blendshapes nicht verfügbar";
    }
}

public struct FacialBlendshapes
{
    public float JawOpen;
    public float MouthSmileLeft;
    public float MouthSmileRight;
    public float TongueOut;
    public float EyeBlinkLeft;
    public float EyeBlinkRight;
    public float BrowDown;
    public float BrowInnerUp;
    public float CheekPuff;
    public float NoseSneer;

    public override string ToString()
    {
        return $"JawOpen: {JawOpen:F2}\nMouthSmileLeft: {MouthSmileLeft:F2}\nMouthSmileRight: {MouthSmileRight:F2}\nTongueOut: {TongueOut:F2}\nEyeBlinkLeft: {EyeBlinkLeft:F2}\nEyeBlinkRight: {EyeBlinkRight:F2}\nBrowDown: {BrowDown:F2}\nBrowInnerUp: {BrowInnerUp:F2}\nCheekPuff: {CheekPuff:F2}\nNoseSneer: {NoseSneer:F2}";
    }
}