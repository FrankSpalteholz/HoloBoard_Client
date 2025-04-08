// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
  public class HandLandmarkerRunner : VisionTaskApiRunner<HandLandmarker>
  {
    [SerializeField] private HandLandmarkerResultAnnotationController _handLandmarkerResultAnnotationController;

    private Experimental.TextureFramePool _textureFramePool;

    public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig();

    public override void Stop()
    {
      base.Stop();
      _textureFramePool?.Dispose();
      _textureFramePool = null;
    }

    protected override IEnumerator Run()
    {
      Debug.Log($"[HandLandmarker] Starting on platform: {Application.platform} with config:");
      Debug.Log($"[HandLandmarker] Delegate = {config.Delegate}");
      Debug.Log($"[HandLandmarker] Image Read Mode = {config.ImageReadMode}");
      Debug.Log($"[HandLandmarker] Running Mode = {config.RunningMode}");
      Debug.Log($"[HandLandmarker] NumHands = {config.NumHands}");
      Debug.Log($"[HandLandmarker] MinHandDetectionConfidence = {config.MinHandDetectionConfidence}");
      Debug.Log($"[HandLandmarker] MinHandPresenceConfidence = {config.MinHandPresenceConfidence}");
      Debug.Log($"[HandLandmarker] MinTrackingConfidence = {config.MinTrackingConfidence}");

      Debug.Log($"[HandLandmarker] Preparing asset: {config.ModelPath}");
      yield return AssetLoader.PrepareAssetAsync(config.ModelPath);
      Debug.Log($"[HandLandmarker] Asset prepared successfully");

      var options = config.GetHandLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnHandLandmarkDetectionOutput : null);
      taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
      Debug.Log($"[HandLandmarker] HandLandmarker created successfully");
      
      var imageSource = ImageSourceProvider.ImageSource;
      Debug.Log($"[HandLandmarker] ImageSource: {imageSource.GetType().Name}, Name={imageSource.sourceName}, IsFrontFacing={imageSource.isFrontFacing}");

      Debug.Log($"[HandLandmarker] Starting ImageSource...");
      yield return imageSource.Play();

      if (!imageSource.isPrepared)
      {
        Debug.LogError($"[HandLandmarker] Failed to start ImageSource on {Application.platform}, exiting...");
        yield break;
      }

      Debug.Log($"[HandLandmarker] ImageSource started successfully: {imageSource.textureWidth}x{imageSource.textureHeight}");

      // Use RGBA32 as the input format.
      // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so maybe the following code needs to be fixed.
      Debug.Log($"[HandLandmarker] Creating TextureFramePool");
      _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

      // NOTE: The screen will be resized later, keeping the aspect ratio.
      Debug.Log($"[HandLandmarker] Initializing screen");
      screen.Initialize(imageSource);

      Debug.Log($"[HandLandmarker] Setting up annotation controller");
      SetupAnnotationController(_handLandmarkerResultAnnotationController, imageSource);

      var transformationOptions = imageSource.GetTransformationOptions();
      var flipHorizontally = transformationOptions.flipHorizontally;
      var flipVertically = transformationOptions.flipVertically;
      var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle);
      Debug.Log($"[HandLandmarker] Transform options - flipH: {flipHorizontally}, flipV: {flipVertically}, rotation: {transformationOptions.rotationAngle}");

      AsyncGPUReadbackRequest req = default;
      var waitUntilReqDone = new WaitUntil(() => req.done);
      var waitForEndOfFrame = new WaitForEndOfFrame();
      var result = HandLandmarkerResult.Alloc(options.numHands);

      // NOTE: we can share the GL context of the render thread with MediaPipe (for now, only on Android)
      var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
      Debug.Log($"[HandLandmarker] GraphicsDeviceType: {SystemInfo.graphicsDeviceType}, canUseGpuImage: {canUseGpuImage}");
      using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

      Debug.Log($"[HandLandmarker] Starting main detection loop");
      while (true)
      {
        if (isPaused)
        {
          yield return new WaitWhile(() => isPaused);
        }

        if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
        {
          yield return new WaitForEndOfFrame();
          continue;
        }

        // Build the input Image
        Image image;
        switch (config.ImageReadMode)
        {
          case ImageReadMode.GPU:
            if (!canUseGpuImage)
            {
              Debug.LogError("[HandLandmarker] ImageReadMode.GPU is not supported on this platform");
              throw new System.Exception("ImageReadMode.GPU is not supported");
            }
            textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            image = textureFrame.BuildGPUImage(glContext);
            // TODO: Currently we wait here for one frame to make sure the texture is fully copied to the TextureFrame before sending it to MediaPipe.
            // This usually works but is not guaranteed. Find a proper way to do this. See: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
            yield return waitForEndOfFrame;
            break;
          case ImageReadMode.CPU:
            yield return waitForEndOfFrame;
            textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;
          case ImageReadMode.CPUAsync:
          default:
            req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            yield return waitUntilReqDone;

            if (req.hasError)
            {
              Debug.LogWarning($"[HandLandmarker] Failed to read texture from the image source");
              continue;
            }
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;
        }

        switch (taskApi.runningMode)
        {
          case Tasks.Vision.Core.RunningMode.IMAGE:
            if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
            {
              _handLandmarkerResultAnnotationController.DrawNow(result);
            }
            else
            {
              _handLandmarkerResultAnnotationController.DrawNow(default);
            }
            break;
          case Tasks.Vision.Core.RunningMode.VIDEO:
            if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
            {
              _handLandmarkerResultAnnotationController.DrawNow(result);
            }
            else
            {
              _handLandmarkerResultAnnotationController.DrawNow(default);
            }
            break;
          case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
            taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
            break;
        }
      }
    }

    private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
    {
      _handLandmarkerResultAnnotationController.DrawLater(result);
    }
  }
}