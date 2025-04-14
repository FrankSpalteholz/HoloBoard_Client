using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Mediapipe.Unity;

public class ARFoundationImageSource : ImageSource
{
    public ARCameraManager arCameraManager;
    private Texture2D _texture;
    private bool _isPrepared = false;
    private bool _isPlaying = false;
    private bool _hasLoggedResolution = false;

    private const int TargetWidth = 1280;
    private const int TargetHeight = 720;


    public override string sourceName => "ARFoundation Camera";
    public override string[] sourceCandidateNames => new string[] { "ARCamera" };
    public override ResolutionStruct[] availableResolutions => new ResolutionStruct[] {
        new ResolutionStruct(1440, 1080, 30)
    };

    public override bool isPrepared => _isPrepared;
    public override bool isPlaying => _isPlaying;

    public override Texture GetCurrentTexture()
    {
        Debug.Log($"[ARImageSource] GetCurrentTexture called: {_texture?.width}x{_texture?.height}");
        return _texture;
    }


    public ARFoundationImageSource(ARCameraManager cameraManager)
    {
        arCameraManager = cameraManager;
        arCameraManager.frameReceived += OnCameraFrameReceived;
    }


    public override void SelectSource(int sourceId)
    {
        // Für ARFoundation brauchst du nichts machen, nur Dummy
    }

    public override IEnumerator Play()
    {
        _isPrepared = true;
        _isPlaying = true;

        // Warte auf den ersten Frame mit gültiger Textur
        float timeout = 5f;
        float elapsed = 0f;

        while (_texture == null && elapsed < timeout)
        {
            Debug.Log("[ARImageSource] Waiting for first texture...");
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (_texture != null)
        {
            Debug.Log($"[ARImageSource] First texture ready: {_texture.width}x{_texture.height}");
        }
        else
        {
            Debug.LogWarning("[ARImageSource] No texture received in time!");
        }
    }


    public override IEnumerator Resume()
    {
        _isPlaying = true;
        yield return null;
    }

    public override void Pause()
    {
        _isPlaying = false;
    }

    public override void Stop() 
    {
        _isPlaying = false;
        _isPrepared = false;

        if (arCameraManager != null)
        {
            arCameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }


    void OnEnable()
    {
        if (arCameraManager != null)
            arCameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        if (arCameraManager != null)
            arCameraManager.frameReceived -= OnCameraFrameReceived;
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (!_isPlaying) return;

        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
        {
            Debug.LogWarning("[ARImageSource] Could not acquire CPU image.");
            return;
        }

        try
        {
            int sourceWidth = cpuImage.width;
            int sourceHeight = cpuImage.height;

            float targetAspect = (float)TargetWidth / TargetHeight;
            int cropHeight = sourceHeight;
            int cropWidth = Mathf.RoundToInt(cropHeight * targetAspect);

            if (cropWidth > sourceWidth)
            {
                cropWidth = sourceWidth;
                cropHeight = Mathf.RoundToInt(cropWidth / targetAspect);
            }

            int offsetX = (sourceWidth - cropWidth) / 2;
            int offsetY = (sourceHeight - cropHeight) / 2;

            if (_texture == null || _texture.width != TargetWidth || _texture.height != TargetHeight)
            {
                _texture = new Texture2D(TargetWidth, TargetHeight, TextureFormat.RGBA32, false);
            }

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(offsetX, offsetY, cropWidth, cropHeight),
                outputDimensions = new Vector2Int(TargetWidth, TargetHeight),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.MirrorY
            };

            cpuImage.Convert(conversionParams, _texture.GetRawTextureData<byte>());
            _texture.Apply();

            if (!_hasLoggedResolution)
            {
                Debug.Log($"[ARImageSource] CPU Image: {sourceWidth}x{sourceHeight} → Cropped: {cropWidth}x{cropHeight} → {_texture.width}x{_texture.height}");
                _hasLoggedResolution = true;
            }
        }
        finally
        {
            cpuImage.Dispose();
        }
    }

    public void Enable()
    {
        if (arCameraManager != null)
            arCameraManager.frameReceived += OnCameraFrameReceived;
    }

    public void Disable()
    {
        if (arCameraManager != null)
            arCameraManager.frameReceived -= OnCameraFrameReceived;
    }

  

}
