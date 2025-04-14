using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Mediapipe.Unity;

public class ARFoundationSourceComponent : MonoBehaviour
{
    //public ARCameraManager arCameraManager;

    [SerializeField] private ARCameraManager arCameraManager;

    [SerializeField] private bool showTextureOnMaterial = false;
    [SerializeField] private Renderer targetRenderer;

    private ARFoundationImageSource _imageSource;
    public ARFoundationImageSource ImageSource => _imageSource;

    void Awake()
    {
        _imageSource = new ARFoundationImageSource(arCameraManager);
    }


    // private void Awake()
    // {
    //     if (arCameraManager == null)
    //     {
    //         arCameraManager = FindObjectOfType<ARCameraManager>();
    //     }

    //     _source = new ARFoundationImageSource(arCameraManager);
    // }

    void Update()
    {
        if (showTextureOnMaterial && targetRenderer != null && _imageSource != null)
        {
            var tex = _imageSource.GetCurrentTexture();
            if (tex != null)
            {
                targetRenderer.material.mainTexture = tex;
            }
        }
    }


    void OnEnable()
    {
        _imageSource?.Enable();
    }

    void OnDisable()
    {
        _imageSource?.Disable();
    }


    private ARFoundationImageSource _source;

    public ARFoundationImageSource Source => _source;

    
}
