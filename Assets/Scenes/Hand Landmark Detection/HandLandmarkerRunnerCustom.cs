using System.Collections;
using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample;
using MPImageFormat = Mediapipe.ImageFormat.Types;
using MPImage = Mediapipe.Image;
using Unity.Collections;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    /// <summary>
    /// Custom implementation of HandLandmarkerRunner with Singleton pattern
    /// and integration with HandLandmarkExtractor
    /// </summary>
    public class HandLandmarkerRunnerCustom : VisionTaskApiRunner<HandLandmarker>
    {
        // Singleton implementation
        public static HandLandmarkerRunnerCustom Instance { get; private set; }

        [SerializeField] private HandLandmarkerResultAnnotationController _handLandmarkerResultAnnotationController;
        
        [SerializeField] 
        [Tooltip("Reference to ARCameraToMediaPipe script")]
        private ARCameraToMediaPipe _arCameraToMediaPipe;
        
        [SerializeField]
        [Tooltip("Reduce resolution for better performance")]
        private bool useReducedResolution = true;
        
        [SerializeField]
        [Tooltip("Width of reduced resolution (default: 640)")]
        private int reducedWidth = 640;
        
        [SerializeField]
        [Tooltip("Height of reduced resolution (default: 360)")]
        private int reducedHeight = 360;
        
        [SerializeField]
        [Tooltip("Show hand landmarks in the scene")]
        private bool showLandmarkAnnotations = true;
        
        [SerializeField]
        [Tooltip("Number of frames a hand persists without re-detection")]
        private int handPersistenceFrames = 5;
        
        [SerializeField]
        [Tooltip("When true, debug text is displayed")]
        private bool showDebugInfo = false;
        
        [SerializeField]
        [Tooltip("GameObject with TextMeshProUGUI for debug information")]
        private TMPro.TextMeshProUGUI debugText;

        private byte[] _imageData;
        private bool _hasNewImageData = false;
        private int _imageWidth = 1280;
        private int _imageHeight = 720;
        
        // Two textures - one for display, one for MediaPipe
        private Texture2D _displayTexture;
        private Texture2D _mediapipeTexture;
        
        // Tracking for hand stability
        private int _framesWithoutHand = 0;
        private HandLandmarkerResult _lastValidResult;

        // Store current HandLandmarkerResult for external access
        private HandLandmarkerResult _currentResult;

        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig();

        private void Awake()
        {
            // Singleton Pattern Implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Register gesture handlers
            RegisterGestureHandlers();
        }
        
        private void RegisterGestureHandlers()
        {
            // Hand detection events
            HandLandmarkExtractor.Instance.OnHandDetected += () => 
            {
                Debug.Log("Hand detected!");
            };
            
            HandLandmarkExtractor.Instance.OnHandLost += () => 
            {
                Debug.Log("Hand lost!");
            };
            
            // Pinch gesture events
            HandLandmarkExtractor.Instance.OnPinchStart += () => 
            {
                Debug.Log("Pinch started!");
            };
            
            HandLandmarkExtractor.Instance.OnPinchEnd += () => 
            {
                Debug.Log("Pinch ended!");
            };
            
            // Pointing gesture events
            HandLandmarkExtractor.Instance.OnPointingStart += () => 
            {
                Debug.Log("Pointing started!");
            };
            
            HandLandmarkExtractor.Instance.OnPointingEnd += () => 
            {
                Debug.Log("Pointing ended!");
            };
        }

        protected override IEnumerator Run()
        {
            Debug.Log($"Delegate = {config.Delegate}");
            Debug.Log($"Running Mode = {config.RunningMode}");
            Debug.Log($"NumHands = {config.NumHands}");
            Debug.Log($"MinHandDetectionConfidence = {config.MinHandDetectionConfidence}");
            Debug.Log($"MinHandPresenceConfidence = {config.MinHandPresenceConfidence}");
            Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");

            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetHandLandmarkerOptions(
                config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? 
                OnHandLandmarkDetectionOutput : null
            );
            
            taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources);

            // Make sure ARCameraToMediaPipe script is assigned
            if (_arCameraToMediaPipe == null)
            {
                _arCameraToMediaPipe = FindObjectOfType<ARCameraToMediaPipe>();
                if (_arCameraToMediaPipe == null)
                {
                    Debug.LogError("ARCameraToMediaPipe script not found, exiting...");
                    yield break;
                }
            }

            // Initialize ARCameraToMediaPipe to receive MediaPipe data
            _arCameraToMediaPipe.OnImageProcessed += ReceiveProcessedImage;
            
            // Initialize screen with processed texture dimensions
            InitializeScreen(_imageWidth, _imageHeight);

            // Create textures
            _displayTexture = new Texture2D(_imageWidth, _imageHeight, TextureFormat.RGBA32, false);
            
            // Create MediaPipe texture in reduced resolution if enabled
            int mediapipeWidth = useReducedResolution ? reducedWidth : _imageWidth;
            int mediapipeHeight = useReducedResolution ? reducedHeight : _imageHeight;
            _mediapipeTexture = new Texture2D(mediapipeWidth, mediapipeHeight, TextureFormat.RGBA32, false);
            
            // Set transformation to identity since our texture is already correctly oriented
            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions();
            
            var result = HandLandmarkerResult.Alloc(options.numHands);

            // Wait for first frame to correctly set texture
            yield return new WaitUntil(() => _hasNewImageData && _imageData != null);
            
            // Process first frame
            PrepareTextures(_imageData);
            screen.texture = _displayTexture;

            while (true)
            {
                if (isPaused)
                {
                    yield return new WaitWhile(() => isPaused);
                }
                
                // Wait for new image data from ARCameraToMediaPipe script
                if (!_hasNewImageData || _imageData == null)
                {
                    yield return null;
                    continue;
                }
                
                // Mark data as processed
                _hasNewImageData = false;

                try 
                {
                    // Process textures
                    PrepareTextures(_imageData);
                    
                    // Update display
                    if (screen.texture != _displayTexture)
                    {
                        screen.texture = _displayTexture;
                    }
                    
                    // Use NativeArray from UNFLIPPED texture for MediaPipe
                    NativeArray<byte> rawData = _mediapipeTexture.GetRawTextureData<byte>();
                    int widthStep = _mediapipeTexture.width * 4;

                    // Create MediaPipe image (unflipped)
                    using (var image = new MPImage(
                                    MPImageFormat.Format.Srgba,
                                    _mediapipeTexture.width,
                                    _mediapipeTexture.height,
                                    widthStep,
                                    rawData))
                    {
                        // Check if we're in LIVE_STREAM mode
                        if (taskApi.runningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM)
                        {
                            taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                        }
                        else
                        {
                            // If we're in IMAGE or VIDEO mode
                            if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
                            {
                                // Hand detected
                                _framesWithoutHand = 0;
                                _lastValidResult = result;
                                _currentResult = result;
                                UpdateHandLandmarkExtractor(result);
                                _handLandmarkerResultAnnotationController.DrawNow(result);
                            }
                            else
                            {
                                // No hand detected, but we can keep the last valid hand for some frames
                                _framesWithoutHand++;
                                
                                if (_framesWithoutHand <= handPersistenceFrames && _lastValidResult.handLandmarks != null)
                                {
                                    // Use last valid result for better stability
                                    _handLandmarkerResultAnnotationController.DrawNow(_lastValidResult);
                                    
                                    if (showDebugInfo && debugText != null)
                                    {
                                        debugText.text = $"Using cached hand result ({_framesWithoutHand}/{handPersistenceFrames})";
                                    }
                                }
                                else
                                {
                                    // Too many frames without hand, show nothing more
                                    _currentResult = default;
                                    _handLandmarkerResultAnnotationController.DrawNow(default);
                                    
                                    // Notify HandLandmarkExtractor that no hand is detected anymore
                                    UpdateHandLandmarkExtractor(default);
                                    
                                    if (showDebugInfo && debugText != null)
                                    {
                                        debugText.text = "No hand detected";
                                    }
                                }
                            }
                        }
                    }
                    
                    // Update Debug Information
                    if (showDebugInfo && debugText != null)
                    {
                        UpdateDebugInfo();
                    }
                }
                catch (System.Exception e) 
                {
                    Debug.LogError($"Error processing image: {e.Message}\nStackTrace: {e.StackTrace}");
                }
                
                // Wait briefly to avoid processing every frame
                yield return null;
            }
        }
        
        private void UpdateDebugInfo()
        {
            if (!HandLandmarkExtractor.Instance.IsHandDetected)
            {
                debugText.text = "No Hand Detected";
                return;
            }
            
            string handedness = HandLandmarkExtractor.Instance.CurrentHandedness;
            bool isPinching = HandLandmarkExtractor.Instance.IsPinching;
            bool isPointing = HandLandmarkExtractor.Instance.IsPointing;
            bool isHandOpen = HandLandmarkExtractor.Instance.IsHandOpen;
            bool isNeutral = HandLandmarkExtractor.Instance.IsNeutral;
            
            Vector3 indexTip = HandLandmarkExtractor.Instance.GetIndexFingerTipPosition();
            
            debugText.text = $"Hand: {handedness}\n" +
                           $"Position: ({indexTip.x:F2}, {indexTip.y:F2})\n" +
                           $"Pinching: {isPinching}\n" +
                           $"Pointing: {isPointing}\n" +
                           $"Hand Open: {isHandOpen}\n" +
                           $"Neutral: {isNeutral}";
        }
        
        // This method prepares both textures - one for display (flipped) and one for MediaPipe (unflipped)
        private void PrepareTextures(byte[] imageData)
        {
            // Load data into display texture first
            _displayTexture.LoadRawTextureData(imageData);
            _displayTexture.Apply();
            
            // If using reduced resolution for MediaPipe, scale the image
            if (useReducedResolution)
            {
                // Create temporary RenderTexture for scaling
                RenderTexture rt = RenderTexture.GetTemporary(reducedWidth, reducedHeight, 0, RenderTextureFormat.ARGB32);
                
                // Blit (scale) display texture to RenderTexture
                Graphics.Blit(_displayTexture, rt);
                
                // Save current active RenderTexture
                RenderTexture activeRT = RenderTexture.active;
                
                // Set scaled RenderTexture as active
                RenderTexture.active = rt;
                
                // Copy RenderTexture content into MediaPipe texture
                _mediapipeTexture.ReadPixels(new UnityEngine.Rect(0, 0, reducedWidth, reducedHeight), 0, 0);
                _mediapipeTexture.Apply();
                
                // Restore previous active RenderTexture
                RenderTexture.active = activeRT;
                
                // Release temporary RenderTexture
                RenderTexture.ReleaseTemporary(rt);
            }
            else
            {
                // If no scaling desired, just copy the data
                _mediapipeTexture.LoadRawTextureData(imageData);
                _mediapipeTexture.Apply();
            }
        }
        
        // This method initializes the screen with fixed dimensions
        private void InitializeScreen(int width, int height)
        {
            if (screen != null)
            {
                screen.Resize(width, height);
                screen.Rotate(RotationAngle.Rotation0.Reverse());
                screen.uvRect = new UnityEngine.Rect(0, 0, 1, -1);
            }
        }
        
        // This method is called by ARCameraToMediaPipe script
        public void ReceiveProcessedImage(byte[] imageData, int width, int height)
        {
            // If image dimensions have changed, update textures
            if (width != _imageWidth || height != _imageHeight)
            {
                _imageWidth = width;
                _imageHeight = height;
                
                // Recreate display texture
                if (_displayTexture != null)
                    Destroy(_displayTexture);
                _displayTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                
                // Recreate MediaPipe texture (possibly in reduced resolution)
                if (_mediapipeTexture != null)
                    Destroy(_mediapipeTexture);
                
                int mediapipeWidth = useReducedResolution ? reducedWidth : width;
                int mediapipeHeight = useReducedResolution ? reducedHeight : height;
                _mediapipeTexture = new Texture2D(mediapipeWidth, mediapipeHeight, TextureFormat.RGBA32, false);
                
                // Update screen
                InitializeScreen(width, height);
            }
            
            _imageData = imageData;
            _hasNewImageData = true;
        }

        private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
        {
            if (showLandmarkAnnotations)
            {
                _handLandmarkerResultAnnotationController.DrawLater(result);
            }
            
            _currentResult = result;
            UpdateHandLandmarkExtractor(result);
        }
        
        private void UpdateHandLandmarkExtractor(HandLandmarkerResult result)
        {
            try
            {
                if (result.handLandmarks != null && result.handLandmarks.Count > 0)
                {
                    HandLandmarkExtractor.Instance.UpdateFrom(result.handLandmarks[0]);
                    
                    if (result.handedness != null && result.handedness.Count > 0 && 
                        result.handedness[0].categories != null && result.handedness[0].categories.Count > 0)
                    {
                        HandLandmarkExtractor.Instance.SetHandedness(result.handedness[0].categories[0].categoryName);
                    }
                    else
                    {
                        // Set default value if handedness is not available
                        HandLandmarkExtractor.Instance.SetHandedness("unknown");
                    }
                }
                else
                {
                    // No hand detected - reset all data
                    HandLandmarkExtractor.Instance.ClearHandData();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating hand landmark extractor: {e.Message}");
                
                // Also reset data on errors
                HandLandmarkExtractor.Instance.ClearHandData();
            }
        }
        
        // Getter method to make current result externally accessible
        public HandLandmarkerResult GetHandLandmarkerResult()
        {
            return _currentResult;
        }
        
        public override void Stop()
        {
            base.Stop();
            
            // Remove event handler
            if (_arCameraToMediaPipe != null)
            {
                _arCameraToMediaPipe.OnImageProcessed -= ReceiveProcessedImage;
            }
            
            // Release textures
            if (_displayTexture != null)
            {
                Destroy(_displayTexture);
                _displayTexture = null;
            }
            
            if (_mediapipeTexture != null)
            {
                Destroy(_mediapipeTexture);
                _mediapipeTexture = null;
            }
        }
        
        // Add OnDestroy to ensure singleton is properly cleaned up
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}