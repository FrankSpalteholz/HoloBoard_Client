using UnityEngine;
using TMPro;

public class FaceTransformDebugger : MonoBehaviour
{
    [Header("Visualization")]
    [SerializeField] private GameObject axisVisualizerPrefab;
    [SerializeField] private bool showVisualizer = true;

    [Header("Transform Settings")]
    [SerializeField] private bool applyPosition = true;
    [SerializeField] private bool applyRotation = true;
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private float visualizerScale = 0.1f;

    [Header("Debug Output")]
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private bool showDebugText = true;
    [SerializeField] private float updateInterval = 0.1f;

    [Header("Status Colors")]
    [SerializeField] private Color trackedColor = new Color(0f, 0.5f, 1f);  // Ozeanblau
    [SerializeField] private Color notTrackedColor = new Color(1f, 0.5f, 0f);  // Orange

    // Runtime objects
    private GameObject visualizerInstance;
    private float lastUpdateTime = 0f;

    // Face tracking reference
    private FaceTransformProvider faceProvider;

    private void Start()
    {
        if (axisVisualizerPrefab != null && showVisualizer)
        {
            visualizerInstance = Instantiate(axisVisualizerPrefab);
            visualizerInstance.transform.localScale = Vector3.one * visualizerScale;
            visualizerInstance.SetActive(false);
        }

        if (debugText != null)
        {
            debugText.text = "Waiting for head tracking...";
        }
    }

    private void Update()
    {
        if (faceProvider == null)
        {
            faceProvider = FindObjectOfType<FaceTransformProvider>();
            if (faceProvider == null)
            {
                if (Time.time - lastUpdateTime > updateInterval)
                {
                    if (debugText != null && showDebugText)
                    {
                        debugText.text = "<color=#" + ColorUtility.ToHtmlStringRGB(notTrackedColor) + ">Searching for head tracking...</color>";
                    }
                    lastUpdateTime = Time.time;
                }
                return;
            }

            faceProvider.OnFaceDetected += OnFaceDetected;
            faceProvider.OnFaceLost += OnFaceLost;
        }

        if (faceProvider.IsFaceTracked())
        {
            if (visualizerInstance != null && showVisualizer)
            {
                visualizerInstance.SetActive(true);

                if (applyPosition)
                {
                    visualizerInstance.transform.position = faceProvider.GetFacePosition() + positionOffset;
                }

                if (applyRotation)
                {
                    Quaternion faceRotation = faceProvider.GetFaceRotation();
                    Quaternion offsetRotation = Quaternion.Euler(rotationOffset);
                    visualizerInstance.transform.rotation = faceRotation * offsetRotation;
                }
            }

            if (debugText != null && showDebugText && Time.time - lastUpdateTime > updateInterval)
            {
                UpdateDebugText();
                lastUpdateTime = Time.time;
            }
        }
    }

    private void OnFaceDetected()
    {
        if (visualizerInstance != null)
        {
            visualizerInstance.SetActive(true);
        }

        if (debugText != null && showDebugText)
        {
            debugText.text = "<color=#" + ColorUtility.ToHtmlStringRGB(trackedColor) + ">Head detected!</color>";
        }
    }

    private void OnFaceLost()
    {
        if (visualizerInstance != null)
        {
            visualizerInstance.SetActive(false);
        }

        if (debugText != null && showDebugText)
        {
            debugText.text = "<color=#" + ColorUtility.ToHtmlStringRGB(notTrackedColor) + ">Head tracking lost</color>";
        }
    }

    private void UpdateDebugText()
    {
        if (debugText == null || !showDebugText || faceProvider == null)
            return;

        Vector3 position = faceProvider.GetFacePosition();
        Vector3 rotation = faceProvider.GetFaceRotation().eulerAngles;

        debugText.text = string.Format(
            "<b>Head Transform Data</b>\n" +
            "Pos: X: {0:F2}  Y: {1:F2}  Z: {2:F2}\n" +
            "Rot: X: {3:F1}°  Y: {4:F1}°  Z: {5:F1}°\n" +
            "Status: <color=#{6}>Tracked</color>",
            position.x, position.y, position.z,
            rotation.x, rotation.y, rotation.z,
            ColorUtility.ToHtmlStringRGB(trackedColor)
        );
    }

    private void OnDestroy()
    {
        if (faceProvider != null)
        {
            faceProvider.OnFaceDetected -= OnFaceDetected;
            faceProvider.OnFaceLost -= OnFaceLost;
        }

        if (visualizerInstance != null)
        {
            Destroy(visualizerInstance);
        }
    }

    public void ToggleVisualizer()
    {
        showVisualizer = !showVisualizer;
        if (visualizerInstance != null)
        {
            visualizerInstance.SetActive(showVisualizer && faceProvider != null && faceProvider.IsFaceTracked());
        }
    }

    public void SetVisualizerScale(float scale)
    {
        visualizerScale = scale;
        if (visualizerInstance != null)
        {
            visualizerInstance.transform.localScale = Vector3.one * visualizerScale;
        }
    }

    public void SetPositionOffset(Vector3 offset)
    {
        positionOffset = offset;
    }

    public void SetRotationOffset(Vector3 offset)
    {
        rotationOffset = offset;
    }
}
